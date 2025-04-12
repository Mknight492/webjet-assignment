import React from "react";
import MovieCard from "../MovieCard";
import { MovieComparison } from "../../types/Movie";
import SkeletonCard from "../LoadingStates/SkeletonCard";
import ErrorMessage from "../ErrorStates/ErrorMessage";

interface MovieGridProps {
  movies: MovieComparison[];
  isLoading: boolean;
  isError: boolean;
  error: Error | null;
  refetch: () => void;
}

const MovieGrid: React.FC<MovieGridProps> = ({ 
  movies, 
  isLoading, 
  isError, 
  error, 
  refetch 
}) => {
  // Generate skeleton cards for loading state
  const skeletonCards = Array(12).fill(0).map((_, index) => (
    <SkeletonCard key={`skeleton-${index}`} />
  ));

  if (isError) {
    return (
      <ErrorMessage 
        message={error?.message || "Failed to load movies"} 
        onRetry={refetch} 
      />
    );
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-6">
      {isLoading
        ? skeletonCards
        : movies.map((movie) => (
            <MovieCard key={movie.id} movie={movie} />
          ))}

      {!isLoading && movies.length === 0 && (
        <div className="col-span-full text-center py-12">
          <p className="text-gray-500 text-lg">No movies available</p>
        </div>
      )}
    </div>
  );
};

export default MovieGrid; 