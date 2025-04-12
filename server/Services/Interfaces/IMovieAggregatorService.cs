using MoviePriceComparison.Models;

namespace MoviePriceComparison.Services.Interfaces;

public interface IMovieAggregatorService
{
    /// <summary>
    /// Gets all movies from all providers
    /// </summary>
    Task<ServiceResponse<List<Movie>>> GetAllMoviesAsync();
    
    /// <summary>
    /// Gets movie details with price comparison
    /// </summary>
    /// <param name="id">The movie ID (includes provider prefix)</param>
    Task<ServiceResponse<MovieDetails>> GetMovieDetailsAsync(string id);
    
    // Update to include separate types for different data
    IAsyncEnumerable<object> StreamMoviesAsync(CancellationToken cancellationToken = default);
} 