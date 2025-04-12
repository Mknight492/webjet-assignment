using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MoviePriceComparison.Configuration;
using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;

namespace MoviePriceComparison.Services;

public class CinemaworldService : IMovieService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CinemaworldService> _logger;
    private readonly MovieApiOptions _options;
    
    public string ProviderName => "Cinemaworld";
    
    public CinemaworldService(
        IHttpClientFactory httpClientFactory,
        ILogger<CinemaworldService> logger,
        IOptions<MovieApiOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("Cinemaworld");
        _logger = logger;
        _options = options.Value;
    }
    
    public async Task<ServiceResponse<List<Movie>>> GetMoviesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching movies from Cinemaworld API");
            var response = await _httpClient.GetFromJsonAsync<MovieResponse>("movies");
            
            if (response?.Movies == null)
            {
                return ServiceResponse<List<Movie>>.FromError("No movies found", ProviderName);
            }
            
            var movies = response.Movies
                .Select(m => Movie.FromMovieItem(m, ProviderName))
                .ToList();
            
            return ServiceResponse<List<Movie>>.FromSuccess(movies, ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movies from Cinemaworld");
            return ServiceResponse<List<Movie>>.FromError($"Error: {ex.Message}", ProviderName);
        }
    }
    
    public async Task<ServiceResponse<MovieDetails>> GetMovieDetailsAsync(string id)
    {
        try
        {
            _logger.LogInformation("Fetching movie details from Cinemaworld API for ID: {Id}", id);
            var response = await _httpClient.GetFromJsonAsync<MovieDetailsResponse>($"movie/{id}");
            
            if (response == null)
            {
                return ServiceResponse<MovieDetails>.FromError("Movie not found", ProviderName);
            }
            
            var movieDetails = MovieDetails.FromMovieDetailsResponse(response, ProviderName);
            
            return ServiceResponse<MovieDetails>.FromSuccess(movieDetails, ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movie details from Cinemaworld for ID: {Id}", id);
            return ServiceResponse<MovieDetails>.FromError($"Error: {ex.Message}", ProviderName);
        }
    }
} 