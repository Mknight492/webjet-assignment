using MoviePriceComparison.Models;

namespace MoviePriceComparison.Services.Interfaces;

public interface IFilmworldApi
{
    Task<MovieResponse> GetMoviesAsync();
    
    Task<MovieDetailsResponse> GetMovieDetailsAsync(string id);
} 