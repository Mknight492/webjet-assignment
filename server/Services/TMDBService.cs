using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MoviePriceComparison.Configuration;
using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;
using System.Text.Json.Serialization;

namespace MoviePriceComparison.Services;

public class TMDBService : ITMDBService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TMDBService> _logger;
    private readonly TMDBOptions _options;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _cacheOptions;
    
    public TMDBService(
        IHttpClientFactory httpClientFactory,
        ILogger<TMDBService> logger,
        IOptions<TMDBOptions> options,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _httpClient = httpClientFactory.CreateClient("TMDB");
        _logger = logger;
        _options = options.Value;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }
    
    public async Task<string> GetPosterUrlAsync(string movieTitle, int? year = null)
    {
        // Try to get from cache first
        string cacheKey = $"TMDB_Poster_{movieTitle}_{year}";
        if (_cache.TryGetValue(cacheKey, out string cachedPosterUrl) && !string.IsNullOrEmpty(cachedPosterUrl))
        {
            _logger.LogInformation("Retrieved poster URL from cache for {MovieTitle}", movieTitle);
            return cachedPosterUrl;
        }
        
        try
        {
            _logger.LogInformation("Searching for movie poster on TMDB: {MovieTitle}", movieTitle);
            
            // Build the search query
            var queryParams = new Dictionary<string, string>
            {
                ["api_key"] = _options.ApiKey,
                ["query"] = movieTitle,
                ["include_adult"] = "false"
            };
            
            if (year.HasValue)
            {
                queryParams["year"] = year.Value.ToString();
            }
            
            var queryString = string.Join("&", queryParams.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
            var response = await _httpClient.GetFromJsonAsync<TMDBSearchResponse>($"search/movie?{queryString}");
            
            if (response?.Results == null || !response.Results.Any())
            {
                _logger.LogWarning("No results found on TMDB for {MovieTitle}", movieTitle);
                return string.Empty;
            }
            
            // Get the first result's poster path
            var movie = response.Results.First();
            if (string.IsNullOrEmpty(movie.PosterPath))
            {
                _logger.LogWarning("No poster found on TMDB for {MovieTitle}", movieTitle);
                return string.Empty;
            }
            
            var posterUrl = $"{_options.ImageBaseUrl}w500{movie.PosterPath}";
            
            // Cache the poster URL
            _cache.Set(cacheKey, posterUrl, _cacheOptions.PosterCacheDuration);
            _logger.LogInformation("Cached poster URL for {MovieTitle} for {Duration}", 
                movieTitle, _cacheOptions.PosterCacheDuration);
                
            return posterUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching poster from TMDB for {MovieTitle}", movieTitle);
            return string.Empty;
        }
    }
    
    public async Task<Movie> EnrichMovieWithTMDBDataAsync(Movie movie)
    {
        var posterUrl = await GetPosterUrlAsync(movie.Title, movie.Year);
        
        if (!string.IsNullOrEmpty(posterUrl))
        {
            movie.Poster = posterUrl;
        }
        
        return movie;
    }
    
    public async Task<List<Movie>> EnrichMoviesWithTMDBDataAsync(List<Movie> movies)
    {
        var enrichedMovies = new List<Movie>();
        
        foreach (var movie in movies)
        {
            enrichedMovies.Add(await EnrichMovieWithTMDBDataAsync(movie));
        }
        
        return enrichedMovies;
    }
    
    public async Task<MovieDetails> EnrichMovieDetailsWithTMDBDataAsync(MovieDetails movieDetails)
    {
        var posterUrl = await GetPosterUrlAsync(movieDetails.Title, movieDetails.Year);
        
        if (!string.IsNullOrEmpty(posterUrl))
        {
            movieDetails.Poster = posterUrl;
        }
        
        return movieDetails;
    }
}

public class TMDBSearchResponse
{
    [JsonPropertyName("results")]
    public List<TMDBMovie> Results { get; set; } = new List<TMDBMovie>();
}

public class TMDBMovie
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }
    
    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }
}