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
    
    public MovieAggregatorService(
        IEnumerable<IMovieService> movieServices,
        ILogger<MovieAggregatorService> logger)
    {
        _movieServices = movieServices;
        _logger = logger;
    }
    
    public async Task<ServiceResponse<List<Movie>>> GetAllMoviesAsync()
    {
        _logger.LogInformation("Fetching movies from all providers");
        
        var movies = new List<Movie>();
        var errors = new Dictionary<string, string>();
        
        // Fetch movies from all providers in parallel
        var tasks = _movieServices.Select(service => service.GetMoviesAsync());
        var results = await Task.WhenAll(tasks);
        
        // Combine results
        foreach (var result in results)
        {
            if (result.Success && result.Data != null)
            {
                movies.AddRange(result.Data);
            }
            else
            {
                errors[result.Source] = result.Message;
                _logger.LogWarning("Error from {Provider}: {Message}", result.Source, result.Message);
            }
        }
        
        // If all providers failed, return an error
        if (movies.Count == 0 && errors.Count > 0)
        {
            return ServiceResponse<List<Movie>>.FromError(
                "Failed to fetch movies from all providers", "Aggregator");
        }
        
        _logger.LogInformation("Retrieved {Count} movies from {SuccessCount} providers",
            movies.Count, results.Count(r => r.Success));
        
        var response = ServiceResponse<List<Movie>>.FromSuccess(movies, "Aggregated", false);
        
        // Add any errors to the message if some providers failed
        if (errors.Count > 0)
        {
            response.Message = $"Some providers failed: {string.Join(", ", errors.Keys)}";
        }
        
        return response;
    }
    
    public async Task<ServiceResponse<MovieDetails>> GetMovieDetailsAsync(string id)
    {
        _logger.LogInformation("Fetching movie details for ID: {Id}", id);
        
        // Determine which provider to use based on the ID prefix
        string providerName = id.StartsWith("fw") ? "Filmworld" : "Cinemaworld";
        
        var service = _movieServices.FirstOrDefault(s => s.ProviderName == providerName);
        
        if (service == null)
        {
            return ServiceResponse<MovieDetails>.FromError(
                $"No service found for provider: {providerName}", "Aggregator");
        }
        
        var result = await service.GetMovieDetailsAsync(id);
        
        // Try the other provider if this one failed
        if (!result.Success)
        {
            _logger.LogWarning("Failed to fetch movie details from {Provider}, trying other providers",
                providerName);
            
            foreach (var fallbackService in _movieServices.Where(s => s.ProviderName != providerName))
            {
                // This is just a simple example - in reality, you'd need ID mapping between providers
                var fallbackResult = await fallbackService.GetMovieDetailsAsync(id);
                if (fallbackResult.Success)
                {
                    _logger.LogInformation("Found movie details in fallback provider: {Provider}",
                        fallbackService.ProviderName);
                    return fallbackResult;
                }
            }
        }
        
        return result;
    }

    public async IAsyncEnumerable<object> StreamMoviesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting to stream movies from all providers");
        
        // First stream movies from whichever provider responds first
        var providerTasks = _movieServices.ToDictionary(
            service => service.ProviderName,
            service => service.GetMoviesAsync()
        );
        
        var allMoviesById = new Dictionary<string, Movie>();
        
        // Wait for any task to complete and immediately yield its result
        while (providerTasks.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var completedTask = await Task.WhenAny(providerTasks.Values);
            
            // Find which provider this task belongs to
            var providerName = providerTasks.First(kv => kv.Value == completedTask).Key;
            providerTasks.Remove(providerName);
            
            var result = await completedTask;
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
        
        // Now fetch and stream details for each movie in parallel (up to 10 at once)
        _logger.LogInformation("Streaming movie details for {Count} movies", allMoviesById.Count);
        
        // Use a semaphore to limit concurrency to 10
        using var semaphore = new SemaphoreSlim(10);
        var runningTasks = new List<Task<ServiceResponse<MovieDetails>?>>();
        var movieQueue = new Queue<KeyValuePair<string, Movie>>(allMoviesById);
        
        // Helper method to create a task for fetching movie details
        async Task<ServiceResponse<MovieDetails>?> FetchMovieDetailsTask(KeyValuePair<string, Movie> moviePair)
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                
                string movieId = moviePair.Key;
                Movie movie = moviePair.Value;
                
                // Determine the provider for this movie
                string providerName = movie.Provider;
                var service = _movieServices.FirstOrDefault(s => s.ProviderName == providerName);
                
                if (service != null)
                {
                    // Use Polly to handle retries and failures
                    var policy = Policy
                        .Handle<Exception>()
                        .WaitAndRetryAsync(
                            3,
                            retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt)),
                            (ex, timeSpan, retryAttempt, ctx) =>
                            {
                                _logger.LogWarning(ex, 
                                    "Error fetching details for movie {Id} from {Provider}, retry attempt {Attempt}", 
                                    movieId, providerName, retryAttempt);
                            });
                    
                    return await policy.ExecuteAsync(() => FetchMovieDetailsAsync(service, movieId, providerName));
                }
                
                return null;
            }
            finally
            {
                semaphore.Release();
            }
        }
        
        // Start initial batch of tasks (up to 10)
        while (movieQueue.Count > 0 && runningTasks.Count < 10 && !cancellationToken.IsCancellationRequested)
        {
            var moviePair = movieQueue.Dequeue();
            runningTasks.Add(FetchMovieDetailsTask(moviePair));
        }
        
        // Process tasks as they complete and start new ones
        while (runningTasks.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var completedTask = await Task.WhenAny(runningTasks);
            runningTasks.Remove(completedTask);
            
            // Yield the result
            var detailsResponse = await completedTask;
            if (detailsResponse != null)
            {
                yield return detailsResponse;
            }
            
            // Start a new task if there are more movies to process
            if (movieQueue.Count > 0)
            {
                var moviePair = movieQueue.Dequeue();
                runningTasks.Add(FetchMovieDetailsTask(moviePair));
            }
        }
        
        // Now collect all movies and return price information for each
        var allMovies = allMoviesById.Values.ToList();
        
        // Group by title (or another identifier that would match across providers)
        var movieGroups = allMovies.GroupBy(m => m.Title);
        
        // Return each group as a price comparison
        foreach (var group in movieGroups)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            var movies = group.ToList();
            _logger.LogInformation("Streaming price comparison for: {Title}", group.Key);
            
            yield return ServiceResponse<List<Movie>>.FromSuccess(
                movies, 
                "PriceComparison", 
                false);
            
            // Add a small delay to avoid overwhelming the client
            await Task.Delay(100, cancellationToken);
        }
        
        _logger.LogInformation("Completed streaming movies, details, and price comparisons");
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