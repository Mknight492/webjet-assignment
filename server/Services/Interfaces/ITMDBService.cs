using MoviePriceComparison.Models;

namespace MoviePriceComparison.Services.Interfaces;

public interface ITMDBService
{
    Task<string> GetPosterUrlAsync(string movieTitle, int? year = null);
    Task<Movie> EnrichMovieWithTMDBDataAsync(Movie movie);
    Task<List<Movie>> EnrichMoviesWithTMDBDataAsync(List<Movie> movies);
    Task<MovieDetails> EnrichMovieDetailsWithTMDBDataAsync(MovieDetails movieDetails);
}