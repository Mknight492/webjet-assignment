using MoviePriceComparison.Models;

namespace MoviePriceComparison.Services.Interfaces;

public interface IMovieAggregatorService
{    
    // Update to include separate types for different data
    IAsyncEnumerable<object> StreamMoviesAsync(CancellationToken cancellationToken = default);
} 