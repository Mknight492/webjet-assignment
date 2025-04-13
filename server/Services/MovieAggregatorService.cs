using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace MoviePriceComparison.Services;

public class MovieAggregatorService : IMovieAggregatorService
{
    private readonly IEnumerable<IMovieService> _movieServices;
    private readonly ILogger<MovieAggregatorService> _logger;
    
    public MovieAggregatorService(
        IEnumerable<IMovieService> movieServices,
        ILogger<MovieAggregatorService> logger)
    {
        _movieServices = movieServices;
        _logger = logger;
    }

    public async IAsyncEnumerable<object> StreamMoviesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to stream movies from all providers");
        
        // Dictionary to track which providers we've already processed
        var processedProviders = new HashSet<string>();
        var allMoviesById = new Dictionary<string, Movie>();
        
        // Create a task for each provider
        var providerTasks = new List<Task<(string ProviderName, ServiceResponse<List<Movie>> Result)>>();
        
        foreach (var service in _movieServices)
        {
            var task = Task.Run(async () =>
            {
                // Simple direct call to service without retries
                var result = await service.GetMoviesAsync();
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
        
        // Now fetch and stream details for each movie
        _logger.LogInformation("Streaming movie details for {Count} movies", allMoviesById.Count);
        
        // Create a counter for completed movie details
        var completedDetailsCount = 0;
        var successfulDetailsCount = 0;
        
        // Simple channel to pass results
        var channel = System.Threading.Channels.Channel.CreateUnbounded<ServiceResponse<MovieDetails>>();
        
        // Start tasks to fetch movie details
        var tasks = new List<Task>();
        foreach (var moviePair in allMoviesById)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            var task = Task.Run(async () => 
            {
                string movieId = moviePair.Key;
                Movie movie = moviePair.Value;
                
                // Determine the provider for this movie
                string providerName = movie.Provider;
                var service = _movieServices.FirstOrDefault(s => s.ProviderName == providerName);
                
                if (service != null)
                {
                    try 
                    {
                        var result = await service.GetMovieDetailsAsync(movieId);
                        Interlocked.Increment(ref completedDetailsCount);
                        
                        if (result.Success && result.Data != null)
                        {
                            Interlocked.Increment(ref successfulDetailsCount);
                            _logger.LogInformation("Successfully fetched details for movie {Id} ({Title}) - Progress: {Completed}/{Total}", 
                                movieId, result.Data?.Title ?? "Unknown", completedDetailsCount, allMoviesById.Count);
                                
                            await channel.Writer.WriteAsync(
                                ServiceResponse<MovieDetails>.FromSuccess(
                                    result.Data, 
                                    $"{providerName}Details", 
                                    false), 
                                cancellationToken);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to fetch details for movie {Id} from {Provider} - Progress: {Completed}/{Total}", 
                                movieId, providerName, completedDetailsCount, allMoviesById.Count);
                        }
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        Interlocked.Increment(ref completedDetailsCount);
                        _logger.LogError(ex, "Error fetching details for movie {Id} from {Provider}", movieId, providerName);
                    }
                }
                else
                {
                    Interlocked.Increment(ref completedDetailsCount);
                    _logger.LogWarning("No service found for provider {Provider} for movie {Id} - Progress: {Completed}/{Total}", 
                        providerName, movieId, completedDetailsCount, allMoviesById.Count);
                }
            }, cancellationToken);
            
            tasks.Add(task);
        }
        
        // Start a task to close the channel when all detail fetching is complete
        _ = Task.Run(async () => 
        {
            try 
            {
                await Task.WhenAll(tasks);
            }
            finally 
            {
                channel.Writer.Complete();
            }
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
        
        _logger.LogInformation("Movie details processing complete. Processed {Completed}/{Total}, Successful: {Success}, Yielded: {Yielded}", 
            completedDetailsCount, allMoviesById.Count, successfulDetailsCount, detailsYielded);
        
        // The price comparison will now be handled by the frontend
        _logger.LogInformation("Completed streaming movies and details. Price comparison will be handled by frontend.");
    }
} 