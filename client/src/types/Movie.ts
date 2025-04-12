// Movie type as returned by the movie list endpoint
export interface Movie {
  Id: string;
  Title: string;
  Year: number;
  Type?: string;
  Poster?: string;
  Provider: string;
  Plot?: string;
  Rated?: string;
  Released?: string;
  Runtime?: string;
  Genre?: string;
  Director?: string;
  Writer?: string;
  Actors?: string;
  Price?: number;
}

// Movie details type as returned by the movie details endpoint
export interface MovieDetail extends Omit<Movie, 'Price'> {
  Rating?: string;
  Price: number; // Required in MovieDetails
}

// Movie with price comparison information
export interface MovieComparison {
  id: string;
  title: string;
  year: string;
  poster?: string;
  prices: {
    cinemaworld?: string;
    filmworld?: string;
  };
  cheapestProvider: 'cinemaworld' | 'filmworld' | null;
}
