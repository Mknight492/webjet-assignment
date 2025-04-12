using MoviePriceComparison.Models;

namespace MoviePriceComparison.Services.Interfaces;

public interface IMovieService
{
    /// <summary>
    /// Gets all movies from a provider
    /// </summary>
    Task<ServiceResponse<List<Movie>>> GetMoviesAsync();
    
    /// <summary>
    /// Gets details for a specific movie
    /// </summary>
    /// <param name="id">The movie ID</param>
    Task<ServiceResponse<MovieDetails>> GetMovieDetailsAsync(string id);
    
    /// <summary>
    /// Gets the name of the provider (e.g., "Cinemaworld", "Filmworld")
    /// </summary>
    string ProviderName { get; }
} 