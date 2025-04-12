// Movie type as returned by the movie list endpoint
export interface Movie {
  id: string;
  title: string;
  year: number;
  type?: string;
  poster?: string;
  provider: string;
  plot?: string;
  rated?: string;
  released?: string;
  runtime?: string;
  genre?: string;
  director?: string;
  writer?: string;
  actors?: string;
  price?: number;
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
