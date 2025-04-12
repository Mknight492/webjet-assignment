// Movie type as returned by the movie list endpoint
export interface Movie {
  ID: string;
  Title: string;
  Year: string;
  Poster?: string;
  Type?: string;
  Price?: string;
}

// Movie details type as returned by the movie details endpoint
export interface MovieDetail extends Movie {
  Rated?: string;
  Released?: string;
  Runtime?: string;
  Genre?: string;
  Director?: string;
  Writer?: string;
  Actors?: string;
  Plot?: string;
  Language?: string;
  Country?: string;
  Awards?: string;
  Metascore?: string;
  imdbRating?: string;
  Price: string;
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
