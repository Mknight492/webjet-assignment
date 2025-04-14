using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using MoviePriceComparison.Configuration;
using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;

namespace MoviePriceComparison.Services;

public class FilmworldService : IMovieService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FilmworldService> _logger;
    private readonly MovieApiOptions _options;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _cacheOptions;
    
    public string ProviderName => "Filmworld";
    
    public FilmworldService(
        IHttpClientFactory httpClientFactory,
        ILogger<FilmworldService> logger,
        IOptions<MovieApiOptions> options,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _httpClient = httpClientFactory.CreateClient("Filmworld");
        _logger = logger;
        _options = options.Value;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }
    
    public async Task<ServiceResponse<List<Movie>>> GetMoviesAsync()
    {
        // Try to get from cache first
        string cacheKey = $"{ProviderName}_Movies";
        if (_cache.TryGetValue(cacheKey, out ServiceResponse<List<Movie>> cachedResponse))
        {
            _logger.LogInformation("Retrieved movies from cache for {ProviderName}", ProviderName);
            return cachedResponse;
        }
        
        try
        {
            _logger.LogInformation("Fetching movies from Filmworld API");
            var response = await _httpClient.GetFromJsonAsync<MovieResponse>("movies");
            
            if (response?.Movies == null)
            {
                return ServiceResponse<List<Movie>>.FromError("No movies found", ProviderName);
            }
            
            var movies = response.Movies
                .Select(m => Movie.FromMovieItem(m, ProviderName))
                .ToList();
            
            var serviceResponse = ServiceResponse<List<Movie>>.FromSuccess(movies, ProviderName);
            
            // Only cache successful responses
            _cache.Set(cacheKey, serviceResponse, _cacheOptions.MoviesCacheDuration);
            _logger.LogInformation("Cached movies for {ProviderName} for {Duration}", 
                ProviderName, _cacheOptions.MoviesCacheDuration);
                
            return serviceResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movies from Filmworld");
            return ServiceResponse<List<Movie>>.FromError($"Error: {ex.Message}", ProviderName);
        }
    }
    
    public async Task<ServiceResponse<MovieDetails>> GetMovieDetailsAsync(string id)
    {
        // Try to get from cache first
        string cacheKey = $"{ProviderName}_Movie_{id}";
        if (_cache.TryGetValue(cacheKey, out ServiceResponse<MovieDetails> cachedResponse))
        {
            _logger.LogInformation("Retrieved movie details from cache for {ProviderName}, ID: {Id}", 
                ProviderName, id);
            return cachedResponse;
        }
        
        try
        {
            _logger.LogInformation("Fetching movie details from Filmworld API for ID: {Id}", id);
            var response = await _httpClient.GetFromJsonAsync<MovieDetailsResponse>($"movie/{id}");
            
            if (response == null)
            {
                return ServiceResponse<MovieDetails>.FromError("Movie not found", ProviderName);
            }
            
            var movieDetails = MovieDetails.FromMovieDetailsResponse(response, ProviderName);
            var serviceResponse = ServiceResponse<MovieDetails>.FromSuccess(movieDetails, ProviderName);
            
            // Only cache successful responses
            _cache.Set(cacheKey, serviceResponse, _cacheOptions.MovieDetailsCacheDuration);
            _logger.LogInformation("Cached movie details for {ProviderName}, ID: {Id} for {Duration}", 
                ProviderName, id, _cacheOptions.MovieDetailsCacheDuration);
                
            return serviceResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie details from Filmworld for ID: {Id}", id);
            return ServiceResponse<MovieDetails>.FromError($"Error: {ex.Message}", ProviderName);
        }
    }
} 