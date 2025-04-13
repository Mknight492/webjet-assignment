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
                _logger.LogInformation("Streaming movies from {Provider}", providerName);
                
                // Store movies for later details fetching
                if (result.Success && result.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        allMoviesById[movie.Id] = movie;
                    }
                }
                
                yield return result;
            }
        }
        
        // Now fetch and stream details for each movie in parallel (up to 10 at once)
        _logger.LogInformation("Streaming movie details for {Count} movies", allMoviesById.Count);
        
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
                        return await FetchMovieDetailsAsync(service, movieId, providerName);
                    }
                    
                    return null;
                },
                moviePair => $"Movie {moviePair.Key} from {moviePair.Value.Provider}",
                cancellationToken,
                async result => 
                {
                    if (result != null)
                    {
                        await channel.Writer.WriteAsync(result, cancellationToken);
                    }
                });
                
            // Mark the channel as complete when all processing is done
            channel.Writer.Complete();
        });
        
        // Read from the channel and yield results as they come in
        await foreach (var detailsResponse in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return detailsResponse;
        }
        
        // Wait for the processing task to complete
        await processingTask;
        
        // The price comparison will now be handled by the frontend
        _logger.LogInformation("Completed streaming movies and details. Price comparison will be handled by frontend.");
    }

    // Helper method to fetch movie details inside a try-catch block
    private async Task<ServiceResponse<MovieDetails>?> FetchMovieDetailsAsync(
        IMovieService service, 
        string movieId, 
        string providerName)
    {
        try
        {
            var detailsResponse = await service.GetMovieDetailsAsync(movieId);
            
            if (detailsResponse.Success && detailsResponse.Data != null)
            {
                _logger.LogInformation("Streaming details for movie: {Title} from {Provider}", 
                    detailsResponse.Data.Title, providerName);
                
                // Return the MovieDetails directly, no need for conversion
                return ServiceResponse<MovieDetails>.FromSuccess(
                    detailsResponse.Data, 
                    $"{providerName}Details", 
                    false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching details for movie {Id} from {Provider}", 
                movieId, providerName);
        }
        
        return null;
    }
} 