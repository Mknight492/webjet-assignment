import type { Movie, MovieDetail } from './Movie';

// Re-export existing Movie types
export type { Movie, MovieDetail };

// Add the ServiceResponse type using camelCase
export interface ServiceResponse<T> {
  data: T | null;
  success: boolean;
  message: string | null;
  source: string | null;
  fromCache: boolean;
}

// Rename MovieDetail to MovieDetails to match server-side model
export type MovieDetails = MovieDetail; 