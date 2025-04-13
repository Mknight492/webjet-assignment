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