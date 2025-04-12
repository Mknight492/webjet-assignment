using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;

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
} 