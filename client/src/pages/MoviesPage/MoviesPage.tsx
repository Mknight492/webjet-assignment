import React, { useState, useEffect } from "react";
import MovieGrid from "../../components/MovieGrid";
import FilterControls from "../../components/FilterControls";
import { useStreamingMovies } from "../../hooks/useStreamingMovies";
import { Movie, MovieComparison } from "../../types/Movie";

const MoviesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("title");
  const { providerMovies, movieDetails, priceComparisons, loading, error } = useStreamingMovies();
  
  console.log("providerMovies", providerMovies);
  console.log("movieDetails", movieDetails);
  console.log("priceComparisons", priceComparisons);

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
        cheapestProvider: null
      };
      
      // Add prices from each provider
      movieGroup.forEach(movie => {
        console.log(movie);
        if (movie.provider.toLowerCase() === 'cinemaworld' && movie.price !== undefined) {
          console.log(movie);
          comparison.prices.cinemaworld = movie.price.toString();
        } else if (movie.provider.toLowerCase() === 'filmworld' && movie.price !== undefined) {
          console.log(movie);
          comparison.prices.filmworld = movie.price.toString();
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
        console.log(movie);
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
              cinemaworld: providerForKey === 'cinemaworld' && movie.price !== undefined ? 
                movie.price.toString() : undefined,
              filmworld: providerForKey === 'filmworld' && movie.price !== undefined ? 
                movie.price.toString() : undefined
            },
            cheapestProvider: providerForKey // Only one provider so it's the cheapest
          };
          
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

      <MovieGrid
        movies={filteredAndSortedMovies}
        isLoading={loading}
        isError={!!error}
        error={error ? new Error(error) : null}
        refetch={refetch}
      />
    </div>
  );
};

export default MoviesPage; 