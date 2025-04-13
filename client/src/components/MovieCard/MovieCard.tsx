import React from "react";
import { Link } from "react-router-dom";
import { MovieComparison } from "../../types/Movie";
import { defaultPlaceholder } from "../../utils/imagePlaceholder";

interface MovieCardProps {
  movie: MovieComparison;
}

const MovieCard: React.FC<MovieCardProps> = ({ movie }) => {
  const { title, year, poster, prices, priceLoadingStates, cheapestProvider } =
    movie;

  const renderPrice = (provider: "cinemaworld" | "filmworld") => {
    if (priceLoadingStates[provider]) {
      return (
        <div className="flex items-center">
          <div className="animate-pulse h-4 w-16 bg-gray-200 rounded"></div>
        </div>
      );
    }

    if (prices[provider]) {
      const isLowest =
        cheapestProvider === provider && prices.cinemaworld && prices.filmworld; // Only highlight if both prices exist

      return (
        <span className={`font-semibold ${isLowest ? "text-green-600" : ""}`}>
          ${prices[provider]}
        </span>
      );
    }

    return <span className="text-gray-400">Unavailable</span>;
  };

  return (
    <div className="bg-white rounded-lg shadow-md overflow-hidden transition-transform hover:scale-105 hover:shadow-lg">
      <Link to={`/movie/${movie.id}`}>
        <div className="relative pb-[150%]">
          {poster ? (
            <img
              src={poster}
              alt={`${title} poster`}
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
          <h3 className="text-lg font-semibold truncate mb-1">{title}</h3>
        </Link>
        <p className="text-gray-600 text-sm mb-3">{year}</p>

        <div className="mt-2 space-y-1">
          <div className="flex justify-between text-sm">
            <span className="text-gray-600">Cinemaworld:</span>
            {renderPrice("cinemaworld")}
          </div>
          <div className="flex justify-between text-sm">
            <span className="text-gray-600">Filmworld:</span>
            {renderPrice("filmworld")}
          </div>
        </div>

        {cheapestProvider && (
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
