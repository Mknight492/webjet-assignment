import React, { useState } from "react";
import MovieGrid from "../../components/MovieGrid";
import FilterControls from "../../components/FilterControls";
import { useStreamingMovies } from "../../hooks/useStreamingMovies";
import { Movie, MovieComparison, MovieDetail } from "../../types/Movie";

const MoviesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("title");
  const { 
    providerMovies, 
    movieDetails, 
    priceComparisons, 
    loading, 
    error,
    providerErrors,
    hasPartialData 
  } = useStreamingMovies();
  
  // Transform the streaming data into MovieComparison objects
  const processedMovies: MovieComparison[] = React.useMemo(() => {
    // Start with the price comparison groups which have the most complete data
    const comparisons: MovieComparison[] = [];
    
    // Process price comparison groups first (highest quality data)
    priceComparisons.forEach(movieGroup => {
      if (movieGroup.length === 0) return;
      
      // Group should contain the same movie from different providers
      const firstMovie = movieGroup[0];
      
      const comparison: MovieComparison = {
        id: firstMovie.id,
        title: firstMovie.title,
        year: String(firstMovie.year),
        poster: firstMovie.poster,
        prices: {
          cinemaworld: undefined,
          filmworld: undefined
        },
        priceLoadingStates: {
          cinemaworld: true,
          filmworld: true
        },
        cheapestProvider: null
      };
      
      // Add prices from each provider - only the MovieDetail objects would have prices
      movieGroup.forEach(movie => {
        const movieId = `${movie.provider}-${movie.id}`;
        const detail = movieDetails[movieId];
        const provider = movie.provider.toLowerCase();
        
        if (provider.toLowerCase() === 'cinemaworld') {
          if (detail?.price !== undefined) {
            comparison.prices.cinemaworld = detail.price.toString();
            comparison.priceLoadingStates.cinemaworld = false;
          }
        } else if (provider.toLowerCase() === 'filmworld') {
          if (detail?.price !== undefined) {
            comparison.prices.filmworld = detail.price.toString();
            comparison.priceLoadingStates.filmworld = false;
          }
        }
      });
      
      // Determine cheapest provider
      if (comparison.prices.cinemaworld && comparison.prices.filmworld) {
        const cwPrice = parseFloat(comparison.prices.cinemaworld);
        const fwPrice = parseFloat(comparison.prices.filmworld);
        comparison.cheapestProvider = cwPrice <= fwPrice ? 'cinemaworld' : 'filmworld';
      } else if (comparison.prices.cinemaworld) {
        comparison.cheapestProvider = 'cinemaworld';
      } else if (comparison.prices.filmworld) {
        comparison.cheapestProvider = 'filmworld';
      }
      
      comparisons.push(comparison);
    });
    
    // Fill in any movies we haven't processed yet from provider lists
    // Build a set of titles we've already processed
    const processedTitles = new Set(comparisons.map(m => m.title.toLowerCase()));
    
    // Process any movies from providers that weren't in the comparison groups
    Object.entries(providerMovies).forEach(([provider, movies]) => {
      movies.forEach(movie => {
        if (!processedTitles.has(movie.title.toLowerCase())) {
          // Get the provider name in lowercase for consistency
          const providerKey = provider.toLowerCase();
          const providerForKey = providerKey.includes('cinema') ? 'cinemaworld' : 'filmworld';
          
          // Try to find movie details if available
          const detailKey = `${provider}-${movie.id}`;
          const detail = movieDetails[detailKey];
          
          const comparison: MovieComparison = {
            id: movie.id,
            title: movie.title,
            year: String(movie.year),
            poster: movie.poster,
            prices: {
              cinemaworld: undefined,
              filmworld: undefined
            },
            priceLoadingStates: {
              cinemaworld: providerForKey === 'cinemaworld',
              filmworld: providerForKey === 'filmworld'
            },
            cheapestProvider: providerForKey // Only one provider so it's the cheapest
          };
          
          // Update price if we have it
          if (detail?.price !== undefined) {
            if (providerForKey === 'cinemaworld') {
              comparison.prices.cinemaworld = detail.price.toString();
              comparison.priceLoadingStates.cinemaworld = false;
            } else if (providerForKey === 'filmworld') {
              comparison.prices.filmworld = detail.price.toString();
              comparison.priceLoadingStates.filmworld = false;
            }
          }
          
          comparisons.push(comparison);
          processedTitles.add(movie.title.toLowerCase());
        }
      });
    });
    
    return comparisons;
  }, [providerMovies, movieDetails, priceComparisons]);

  // Filter and sort movies based on user input
  const filteredAndSortedMovies = React.useMemo(() => {
    let result = [...processedMovies];
    
    // Apply search filter
    if (searchTerm.trim()) {
      const normalizedSearchTerm = searchTerm.toLowerCase().trim();
      result = result.filter(movie => 
        movie.title.toLowerCase().includes(normalizedSearchTerm)
      );
    }
    
    // Apply sorting
    switch (sortBy) {
      case "title":
        result.sort((a, b) => a.title.localeCompare(b.title));
        break;
      case "year":
        result.sort((a, b) => {
          const yearA = parseInt(a.year);
          const yearB = parseInt(b.year);
          return yearB - yearA; // newer first
        });
        break;
      case "price-low":
        result.sort((a, b) => {
          const priceA = Math.min(
            a.prices.cinemaworld ? parseFloat(a.prices.cinemaworld) : Infinity,
            a.prices.filmworld ? parseFloat(a.prices.filmworld) : Infinity
          );
          const priceB = Math.min(
            b.prices.cinemaworld ? parseFloat(b.prices.cinemaworld) : Infinity,
            b.prices.filmworld ? parseFloat(b.prices.filmworld) : Infinity
          );
          return priceA - priceB;
        });
        break;
      case "price-high":
        result.sort((a, b) => {
          const priceA = Math.min(
            a.prices.cinemaworld ? parseFloat(a.prices.cinemaworld) : Infinity,
            a.prices.filmworld ? parseFloat(a.prices.filmworld) : Infinity
          );
          const priceB = Math.min(
            b.prices.cinemaworld ? parseFloat(b.prices.cinemaworld) : Infinity,
            b.prices.filmworld ? parseFloat(b.prices.filmworld) : Infinity
          );
          return priceB - priceA;
        });
        break;
      default:
        break;
    }
    
    return result;
  }, [processedMovies, searchTerm, sortBy]);

  // Function to retry fetching if needed
  const refetch = () => {
    // Our SSE endpoint will automatically reconnect,
    // but we can force a page reload as a fallback
    window.location.reload();
  };

  return (
    <div>
      {hasPartialData && Object.keys(providerErrors).length > 0 && (
        <div className="bg-yellow-50 border-l-4 border-yellow-400 p-4 mb-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <p className="text-sm text-yellow-700">
                Some data may be incomplete. Showing the best available information.
                <button onClick={refetch} className="ml-2 font-medium underline text-yellow-700 hover:text-yellow-600">
                  Retry
                </button>
              </p>
            </div>
          </div>
        </div>
      )}

      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Movies</h1>
        
        <div className="relative">
          <input
            type="text"
            placeholder="Search movies..."
            className="py-2 px-4 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
          <div className="absolute right-3 top-2.5 text-gray-400">
            <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clipRule="evenodd" />
            </svg>
          </div>
        </div>
      </div>

      <FilterControls sortBy={sortBy} onSortChange={setSortBy} />

      {loading && !hasPartialData ? (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
          {[...Array(10)].map((_, i) => (
            <div key={i} className="bg-white rounded-lg shadow-md overflow-hidden">
              <div className="animate-pulse">
                <div className="bg-gray-300 h-56 w-full"></div>
                <div className="p-4">
                  <div className="h-4 bg-gray-300 rounded w-3/4 mb-2"></div>
                  <div className="h-3 bg-gray-300 rounded w-1/2 mb-4"></div>
                  <div className="h-3 bg-gray-300 rounded w-full mb-2"></div>
                  <div className="h-3 bg-gray-300 rounded w-full"></div>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : error && !hasPartialData ? (
        <div className="text-center py-10">
          <div className="text-red-500 text-4xl mb-4">
            <svg xmlns="http://www.w3.org/2000/svg" className="h-16 w-16 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h2 className="text-2xl font-bold text-gray-700 mb-2">Unable to Load Movies</h2>
          <p className="text-gray-600 mb-4">
            {error || "We're having trouble connecting to the movie service."}
          </p>
          <button
            onClick={refetch}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Try Again
          </button>
        </div>
      ) : (
        <MovieGrid
          movies={filteredAndSortedMovies}
          isLoading={loading}
          isError={false}
          error={error}
          refetch={refetch}
        />
      )}
    </div>
  );
};

export default MoviesPage; 