// Basic Movie type as returned by the movie list endpoint (after transformation)
export interface Movie {
  id: string;
  title: string;
  year: number;
  type?: string;
  poster?: string;
  provider: string;
  // Basic Movie doesn't have price
}

// Movie details type as returned by the movie details endpoint (after transformation)
export interface MovieDetail extends Movie {
  plot?: string;
  rated?: string;
  released?: string;
  runtime?: string;
  genre?: string;
  director?: string;
  writer?: string;
  actors?: string;
  price: number; // Only MovieDetail has price, and it's required
  rating?: string;
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
  priceLoadingStates: {
    cinemaworld: boolean;
    filmworld: boolean;
  };
  cheapestProvider: 'cinemaworld' | 'filmworld' | null;
}
