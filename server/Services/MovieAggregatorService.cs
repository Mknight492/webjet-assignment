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
    private readonly ITMDBService _tmdbService;
    
    public MovieAggregatorService(
        IEnumerable<IMovieService> movieServices,
        ILogger<MovieAggregatorService> logger,
        ITMDBService tmdbService)
    {
        _movieServices = movieServices;
        _logger = logger;
        _tmdbService = tmdbService;
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
                    
                    // Enrich movies with TMDB data
                    try
                    {
                        var enrichedMovies = await _tmdbService.EnrichMoviesWithTMDBDataAsync(result.Data);
                        result = ServiceResponse<List<Movie>>.FromSuccess(enrichedMovies, providerName, result.FromCache);
                        _logger.LogInformation("Enriched {Count} movies from {Provider} with TMDB data", 
                            enrichedMovies.Count, providerName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to enrich movies from {Provider} with TMDB data, using original posters", providerName);
                    }
                    
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
    }

    public async IAsyncEnumerable<object> StreamMovieDetailsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to stream movie details from all providers");
        
        // Dictionary to track which providers we've already processed
        var processedProviders = new HashSet<string>();
        var allMoviesById = new Dictionary<string, Movie>();
        
        // First collect all movies from all providers
        foreach (var service in _movieServices)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            try
            {
                var result = await service.GetMoviesAsync();
                if (result.Success && result.Data != null)
                {
                    foreach (var movie in result.Data)
                    {
                        allMoviesById[movie.Id] = movie;
                    }
                    
                    processedProviders.Add(service.ProviderName);
                    _logger.LogInformation("Collected {Count} movies from {Provider} for details streaming", 
                        result.Data.Count, service.ProviderName);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger.LogError(ex, "Error collecting movies from {Provider}", service.ProviderName);
            }
        }
        
        _logger.LogInformation("Total unique movies collected for details: {Count}", allMoviesById.Count);
        
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
                            
                            // Enrich with TMDB data
                            if (result.Data != null)
                            {
                                try
                                {
                                    result.Data = await _tmdbService.EnrichMovieDetailsWithTMDBDataAsync(result.Data);
                                    _logger.LogInformation("Enriched movie {Id} ({Title}) with TMDB data", 
                                        movieId, result.Data.Title);
                                }
                                catch (Exception tmdbEx)
                                {
                                    _logger.LogWarning(tmdbEx, "Failed to enrich movie {Id} with TMDB data, using original poster", movieId);
                                }
                            }
                                
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
    }
} 