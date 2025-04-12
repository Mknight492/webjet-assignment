import React, { useState } from "react";
import MovieGrid from "../../components/MovieGrid";
import FilterControls from "../../components/FilterControls";
import useMovies from "../../hooks/useMovies";
import { Movie, MovieComparison } from "../../types/Movie";

const MoviesPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("title");
  const { data, isLoading, isError, error, refetch } = useMovies();

  // Transform the API movie data into the format needed for display
  const processedMovies: MovieComparison[] = React.useMemo(() => {
    if (!data?.Data) return [];

    return data.Data.map((movie: Movie) => {
      // In a real app, we'd get price data here or from a separate hook
      // For now, let's simulate with mock prices
      const cinemaworldPrice = Math.random() > 0.1 ? (10 + Math.random() * 20).toFixed(2) : undefined;
      const filmworldPrice = Math.random() > 0.1 ? (10 + Math.random() * 20).toFixed(2) : undefined;
      
      // Determine the cheapest provider
      let cheapestProvider: "cinemaworld" | "filmworld" | null = null;
      
      if (cinemaworldPrice && filmworldPrice) {
        cheapestProvider = parseFloat(cinemaworldPrice) <= parseFloat(filmworldPrice) 
          ? "cinemaworld" 
          : "filmworld";
      } else if (cinemaworldPrice) {
        cheapestProvider = "cinemaworld";
      } else if (filmworldPrice) {
        cheapestProvider = "filmworld";
      }

      return {
        id: movie.ID,
        title: movie.Title,
        year: movie.Year,
        poster: movie.Poster,
        prices: {
          cinemaworld: cinemaworldPrice,
          filmworld: filmworldPrice,
        },
        cheapestProvider,
      };
    });
  }, [data]);

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
        isLoading={isLoading}
        isError={isError}
        error={error as Error}
        refetch={refetch}
      />
    </div>
  );
};

export default MoviesPage; 