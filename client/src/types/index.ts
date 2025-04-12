import type { Movie, MovieDetail } from './Movie';

// Re-export existing Movie types
export type { Movie, MovieDetail };

// Add the ServiceResponse type
export interface ServiceResponse<T> {
  Data: T | null;
  Success: boolean;
  Message: string | null;
  Source: string | null;
  FromCache: boolean;
}

// Rename MovieDetail to MovieDetails to match server-side model
export type MovieDetails = MovieDetail; 