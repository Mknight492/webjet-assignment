import React from "react";
import { MovieComparison } from "../../types/Movie";

interface MovieGridProps {
  movies: MovieComparison[];
  isLoading: boolean;
  isError: boolean;
  error: string | null;
  refetch: () => void;
  getMovieUrl: (title: string) => string;
}

const MovieGrid: React.FC<MovieGridProps> = ({ 
  movies, 
  isLoading, 
  isError, 
  error, 
  refetch,
  getMovieUrl 
}) => {
  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (isError) {
    return (
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
    );
  }

  if (movies.length === 0) {
    return (
      <div className="text-center py-10">
        <div className="text-gray-400 text-4xl mb-4">
          <svg xmlns="http://www.w3.org/2000/svg" className="h-16 w-16 mx-auto" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
          </svg>
        </div>
        <h2 className="text-2xl font-bold text-gray-700 mb-2">No Movies Found</h2>
        <p className="text-gray-600">Try adjusting your search or filters.</p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
      {movies.map(movie => (
        <div key={movie.title} className="bg-white rounded-lg shadow-md overflow-hidden transition-transform duration-200 hover:shadow-lg hover:-translate-y-1">
          <div className="relative pb-[150%]">
            {movie.poster ? (
              <img src={movie.poster} alt={movie.title} className="absolute h-full w-full object-cover" />
            ) : (
              <div className="absolute h-full w-full bg-gray-200 flex items-center justify-center">
                <svg xmlns="http://www.w3.org/2000/svg" className="h-16 w-16 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 4v16M17 4v16M3 8h4m10 0h4M3 12h18M3 16h4m10 0h4M4 20h16a1 1 0 001-1V5a1 1 0 00-1-1H4a1 1 0 00-1 1v14a1 1 0 001 1z" />
                </svg>
              </div>
            )}
          </div>
          <div className="p-4">
            <h2 className="font-bold text-lg mb-1 line-clamp-2">{movie.title}</h2>
            <p className="text-gray-600 text-sm mb-2">{movie.year}</p>
            
            <div className="mt-2 space-y-2">
              {/* Cinemaworld price */}
              <div className="flex justify-between items-center">
                <span className="text-sm font-medium">Cinemaworld:</span>
                {movie.priceLoadingStates.cinemaworld ? (
                  <div className="h-4 bg-gray-200 animate-pulse rounded w-16"></div>
                ) : movie.prices.cinemaworld ? (
                  <span className={`text-sm font-bold ${movie.cheapestProvider === 'cinemaworld' ? 'text-green-600' : ''}`}>
                    ${parseFloat(movie.prices.cinemaworld).toFixed(2)}
                  </span>
                ) : (
                  <span className="text-xs text-red-500">Unavailable</span>
                )}
              </div>
              
              {/* Filmworld price */}
              <div className="flex justify-between items-center">
                <span className="text-sm font-medium">Filmworld:</span>
                {movie.priceLoadingStates.filmworld ? (
                  <div className="h-4 bg-gray-200 animate-pulse rounded w-16"></div>
                ) : movie.prices.filmworld ? (
                  <span className={`text-sm font-bold ${movie.cheapestProvider === 'filmworld' ? 'text-green-600' : ''}`}>
                    ${parseFloat(movie.prices.filmworld).toFixed(2)}
                  </span>
                ) : (
                  <span className="text-xs text-red-500">Unavailable</span>
                )}
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};

export default MovieGrid; 