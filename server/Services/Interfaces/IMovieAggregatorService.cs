using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MoviePriceComparison.Models;

namespace MoviePriceComparison.Services.Interfaces;

public interface IMovieAggregatorService
{
    /// <summary>
    /// Streams the list of movies from all providers
    /// </summary>
    IAsyncEnumerable<object> StreamMoviesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Streams movie details from all providers
    /// </summary>
    IAsyncEnumerable<object> StreamMovieDetailsAsync(CancellationToken cancellationToken = default);
} 