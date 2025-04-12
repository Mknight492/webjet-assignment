import React from "react";
import { Link } from "react-router-dom";
import { MovieComparison } from "../../types/Movie";
import { defaultPlaceholder } from "../../utils/imagePlaceholder";

interface MovieCardProps {
  movie: MovieComparison;
}

const MovieCard: React.FC<MovieCardProps> = ({ movie }) => {
  // Function to determine the cheapest provider and format the price
  const formatPrice = (price: string | undefined) => {
    if (!price) return "Unavailable";
    return `$${parseFloat(price).toFixed(2)}`;
  };

  return (
    <div className="bg-white rounded-lg shadow-md overflow-hidden transition-transform hover:scale-105 hover:shadow-lg">
      <Link to={`/movie/${movie.id}`}>
        <div className="relative pb-[150%]">
          {movie.poster ? (
            <img
              src={movie.poster}
              alt={movie.title}
              className="absolute h-full w-full object-cover"
              onError={(e) => {
                const target = e.target as HTMLImageElement;
                target.onerror = null;
                target.src = defaultPlaceholder;
              }}
            />
          ) : (
            <div className="absolute h-full w-full flex items-center justify-center bg-gray-200">
              <span className="text-gray-500">No Image</span>
            </div>
          )}
        </div>
      </Link>

      <div className="p-4">
        <Link to={`/movie/${movie.id}`}>
          <h3 className="text-lg font-semibold truncate mb-1">{movie.title}</h3>
        </Link>
        <p className="text-gray-600 text-sm mb-3">{movie.year}</p>

        <div className="mt-2 space-y-1">
          <div className="flex justify-between text-sm">
            <span className="text-gray-600">Cinemaworld:</span>
            <span className={movie.cheapestProvider === "cinemaworld" ? "font-bold text-green-600" : ""}>
              {formatPrice(movie.prices.cinemaworld)}
            </span>
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-gray-600">Filmworld:</span>
            <span className={movie.cheapestProvider === "filmworld" ? "font-bold text-green-600" : ""}>
              {formatPrice(movie.prices.filmworld)}
            </span>
          </div>
        </div>

        {movie.cheapestProvider && (
          <div className="mt-3 text-center">
            <span className="inline-block bg-green-100 text-green-800 text-xs px-2 py-1 rounded">
              Best Price Available
            </span>
          </div>
        )}
      </div>
    </div>
  );
};

export default MovieCard; 