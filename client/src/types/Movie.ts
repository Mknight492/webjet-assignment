// Movie type as returned by the movie list endpoint
export interface Movie {
  Title: string;
  Year: string;
  ID: string;
  Type: string;
  Poster: string;
}

// Movie details type as returned by the movie details endpoint
export interface MovieDetails extends Movie {
  Rated: string;
  Released: string;
  Runtime: string;
  Genre: string;
  Director: string;
  Writer: string;
  Actors: string;
  Plot: string;
  Language: string;
  Country: string;
  Metascore: string;
  Rating: string;
  Votes: string;
  Price: string;
}

// Movie with price comparison information
export interface MovieComparison {
  id: string;
  title: string;
  year: string;
  poster: string;
  prices: {
    cinemaworld?: string;
    filmworld?: string;
  };
  cheapestProvider?: "cinemaworld" | "filmworld" | null;
}
