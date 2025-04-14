using MoviePriceComparison.Models;

namespace MoviePriceComparison.Services.Interfaces;

public interface ICinemaworldApi
{
    Task<MovieResponse> GetMoviesAsync();
    
    Task<MovieDetailsResponse> GetMovieDetailsAsync(string id);
} 