using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Polly;
using Polly.Bulkhead;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;

namespace MoviePriceComparison.Services;

public class MovieAggregatorService : IMovieAggregatorService
{
    private readonly IEnumerable<IMovieService> _movieServices;
    private readonly ILogger<MovieAggregatorService> _logger;
    private readonly IResilienceService _resilienceService;
    
    public MovieAggregatorService(
        IEnumerable<IMovieService> movieServices,
        ILogger<MovieAggregatorService> logger,
        IResilienceService resilienceService)
    {
        _movieServices = movieServices;
        _logger = logger;
        _resilienceService = resilienceService;
    }

    public async IAsyncEnumerable<object> StreamMoviesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to stream movies from all providers");
        
        // Dictionary to track which providers we've already processed
        var processedProviders = new HashSet<string>();
        var allMoviesById = new Dictionary<string, Movie>();
        
        // Create a resilient task for each provider
        var providerTasks = new List<Task<(string ProviderName, ServiceResponse<List<Movie>> Result)>>();
        
        foreach (var service in _movieServices)
        {
            // Use our resilience service to add retry capabilities
            var task = Task.Run(async () =>
            {
                // Use Polly directly with the service name for better control over retries
                var policy = Policy<ServiceResponse<List<Movie>>>
                    .Handle<Exception>()
                    .OrResult(response => !response.Success)  // Also retry on unsuccessful responses
                    .WaitAndRetryAsync(
                        _resilienceService.Options.MaxRetries,
                        retryAttempt => TimeSpan.FromMilliseconds(
                            _resilienceService.Options.InitialRetryDelayMs * 
                            Math.Pow(_resilienceService.Options.RetryBackoffFactor, retryAttempt)),
                        (outcome, timeSpan, retryCount, context) =>
                        {
                            if (outcome.Exception != null)
                            {
                                _logger.LogWarning(outcome.Exception, 
                                    "Error fetching movies from {Provider}, retry attempt {RetryCount} after {Delay}ms",
                                    service.ProviderName, retryCount, timeSpan.TotalMilliseconds);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Unsuccessful response from {Provider}, retry attempt {RetryCount} after {Delay}ms: {ErrorMessage}",
                                    service.ProviderName, retryCount, timeSpan.TotalMilliseconds, 
                                    outcome.Result?.Message ?? "Unknown error");
                            }
                        });
                
                // Execute with the retry policy but don't yield until all retries are done
                var result = await policy.ExecuteAsync(() => service.GetMoviesAsync());
                
                return (service.ProviderName, result);
            });
            
            providerTasks.Add(task);
        }
        
        // Process results as they come in
        while (providerTasks.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            // Wait for any task to complete
            var completedTask = await Task.WhenAny(providerTasks);
            providerTasks.Remove(completedTask);
            
            // Process the result
            var (providerName, result) = await completedTask;
            
            if (!processedProviders.Contains(providerName))
            {
                processedProviders.Add(providerName);
                
                // Store movies for later details fetching
                if (result.Success && result.Data != null)
                {
                    int movieCount = result.Data.Count;
                    _logger.LogInformation("Received {Count} movies from {Provider}", movieCount, providerName);
                    
                    foreach (var movie in result.Data)
                    {
                        allMoviesById[movie.Id] = movie;
                    }
                }
                else
                {
                    _logger.LogWarning("Provider {Provider} returned unsuccessful result: {Message}", 
                        providerName, result.Message);
                }
                
                yield return result;
            }
        }
        
        _logger.LogInformation("Total unique movies collected: {Count}", allMoviesById.Count);
        
        // Now fetch and stream details for each movie in parallel
        _logger.LogInformation("Streaming movie details for {Count} movies", allMoviesById.Count);
        
        // Create a counter for completed movie details
        var completedDetailsCount = 0;
        var successfulDetailsCount = 0;
        
        // Use our resilience service to process movies in parallel with yield return
        var channel = System.Threading.Channels.Channel.CreateUnbounded<ServiceResponse<MovieDetails>>();
        
        // Start the background task to process movies
        var processingTask = Task.Run(async () => 
        {
            await _resilienceService.ExecuteInParallelWithResilienceAsync(
                allMoviesById,
                async (moviePair, ct) => 
                {
                    string movieId = moviePair.Key;
                    Movie movie = moviePair.Value;
                    
                    // Determine the provider for this movie
                    string providerName = movie.Provider;
                    var service = _movieServices.FirstOrDefault(s => s.ProviderName == providerName);
                    
                    if (service != null)
                    {
                        var result = await FetchMovieDetailsAsync(service, movieId, providerName, ct);
                        Interlocked.Increment(ref completedDetailsCount);
                        
                        if (result != null && result.Success)
                        {
                            Interlocked.Increment(ref successfulDetailsCount);
                            _logger.LogInformation("Successfully fetched details for movie {Id} ({Title}) - Progress: {Completed}/{Total}", 
                                movieId, result.Data?.Title ?? "Unknown", completedDetailsCount, allMoviesById.Count);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to fetch details for movie {Id} from {Provider} - Progress: {Completed}/{Total}", 
                                movieId, providerName, completedDetailsCount, allMoviesById.Count);
                        }
                        
                        return result;
                    }
                    
                    Interlocked.Increment(ref completedDetailsCount);
                    _logger.LogWarning("No service found for provider {Provider} for movie {Id} - Progress: {Completed}/{Total}", 
                        providerName, movieId, completedDetailsCount, allMoviesById.Count);
                    
                    return null;
                },
                moviePair => $"Movie {moviePair.Key} from {moviePair.Value.Provider}",
                cancellationToken,
                async result => 
                {
                    if (result != null)
                    {
                      try{
                        await channel.Writer.WriteAsync(result, cancellationToken);
                      } 
                      catch (OperationCanceledException)
                      {
                          _logger.LogInformation("Channel writer was canceled while writing movie details for {Title}", 
                              result.Data?.Title ?? "Unknown");
                      }
                    }
                });
                
            // Mark the channel as complete when all processing is done
            channel.Writer.Complete();
        });
        
        // Read from the channel and yield results as they come in
        var detailsYielded = 0;
        await foreach (var detailsResponse in channel.Reader.ReadAllAsync(cancellationToken))
        {
            detailsYielded++;
            _logger.LogInformation("Yielding movie details for {Title} - {Yielded} details yielded", 
                detailsResponse.Data?.Title ?? "Unknown", detailsYielded);
            yield return detailsResponse;
        }
        
        // Wait for the processing task to complete
        await processingTask;
        
        _logger.LogInformation("Movie details processing complete. Processed {Completed}/{Total}, Successful: {Success}, Yielded: {Yielded}", 
            completedDetailsCount, allMoviesById.Count, successfulDetailsCount, detailsYielded);
        
        // The price comparison will now be handled by the frontend
        _logger.LogInformation("Completed streaming movies and details. Price comparison will be handled by frontend.");
    }

    // Helper method to fetch movie details inside a try-catch block
    private async Task<ServiceResponse<MovieDetails>> FetchMovieDetailsWithRetriesAsync(
        IMovieService service, 
        string movieId, 
        string providerName,
        CancellationToken cancellationToken)
    {
        // Create a custom retry policy that also checks for missing price data
        var policy = Policy<ServiceResponse<MovieDetails>>
            .Handle<Exception>(ex => !(ex is OperationCanceledException)) // Don't retry cancellations
            .OrResult(response => 
                !response.Success || 
                response.Data == null || 
                response.Data.Price <= 0)  // Also retry if price is missing or invalid
            .WaitAndRetryAsync(
                _resilienceService.Options.MaxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(
                    _resilienceService.Options.InitialRetryDelayMs * 
                    Math.Pow(_resilienceService.Options.RetryBackoffFactor, retryAttempt)),
                (outcome, timeSpan, retryCount, context) =>
                {
                    // Check if cancellation is requested before logging
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    
                    if (outcome.Exception != null)
                    {
                        _logger.LogWarning(outcome.Exception, 
                            "Error fetching details for movie {Id} from {Provider}, retry attempt {RetryCount} after {Delay}ms",
                            movieId, providerName, retryCount, timeSpan.TotalMilliseconds);
                    }
                    else if (outcome.Result?.Data == null)
                    {
                        _logger.LogWarning(
                            "No data returned for movie {Id} from {Provider}, retry attempt {RetryCount} after {Delay}ms",
                            movieId, providerName, retryCount, timeSpan.TotalMilliseconds);
                    }
                    else if (outcome.Result.Data.Price <= 0)
                    {
                        _logger.LogWarning(
                            "Missing or invalid price ({Price}) for movie {Id} from {Provider}, retry attempt {RetryCount} after {Delay}ms",
                            outcome.Result.Data.Price, movieId, providerName, retryCount, timeSpan.TotalMilliseconds);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Unsuccessful response for movie {Id} from {Provider}, retry attempt {RetryCount} after {Delay}ms: {ErrorMessage}",
                            movieId, providerName, retryCount, timeSpan.TotalMilliseconds,
                            outcome.Result?.Message ?? "Unknown error");
                    }
                });
        
        try
        {
            // Execute the API call with our specialized retry policy
            // Use the correct Polly API overload for ExecuteAsync with cancellation token
            return await policy.ExecuteAsync(
                (context) => service.GetMovieDetailsAsync(movieId),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Propagate cancellation
            throw;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("Operation was canceled", ex, cancellationToken);
            }
            
            _logger.LogError(ex, "Failed to fetch details for movie {Id} from {Provider} after all retries", 
                movieId, providerName);
            return ServiceResponse<MovieDetails>.FromError(
                $"Failed to fetch details from {providerName} after all retries: {ex.Message}");
        }
    }

    // Replace the FetchMovieDetailsAsync method with this improved version
    private async Task<ServiceResponse<MovieDetails>?> FetchMovieDetailsAsync(
        IMovieService service, 
        string movieId, 
        string providerName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var detailsResponse = await FetchMovieDetailsWithRetriesAsync(service, movieId, providerName, cancellationToken);
            
            if (detailsResponse.Success && detailsResponse.Data != null && detailsResponse.Data.Price > 0)
            {
                _logger.LogInformation("Successfully fetched details for movie: {Title} from {Provider} with price {Price}", 
                    detailsResponse.Data.Title, providerName, detailsResponse.Data.Price);
                
                return ServiceResponse<MovieDetails>.FromSuccess(
                    detailsResponse.Data, 
                    $"{providerName}Details", 
                    false);
            }
            else
            {
                _logger.LogWarning("Failed to fetch valid details for movie {Id} from {Provider} after all retries: {Message}", 
                    movieId, providerName, detailsResponse.Message);
                return null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Request for movie {Id} from {Provider} was canceled", movieId, providerName);
            return null;
        }
    }
} 