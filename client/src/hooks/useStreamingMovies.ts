import { useState, useEffect } from 'react';
import { Movie, MovieDetails, ServiceResponse } from '../types';

export const useStreamingMovies = () => {
  const [providerMovies, setProviderMovies] = useState<Record<string, Movie[]>>({});
  const [movieDetails, setMovieDetails] = useState<Record<string, MovieDetails>>({});
  const [priceComparisons, setPriceComparisons] = useState<Movie[][]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const eventSource = new EventSource(`${process.env.REACT_APP_API_URL || 'https://localhost:7168/api'}/movies/stream`);
    
    eventSource.onmessage = (event) => {
      try {
        const response = JSON.parse(event.data);
        
        if (response.success && response.data) {
          if (response.source === 'PriceComparison') {
            // This is a price comparison group
            setPriceComparisons(prev => [...prev, response.data]);
          } else if (response.source.endsWith('Details')) {
            // This is movie details from a provider - now it's a MovieDetails object
            const movieDetail = response.data;
            const provider = response.source.replace('Details', '');
            
            setMovieDetails(prev => ({
              ...prev,
              [`${provider}-${movieDetail.id}`]: movieDetail
            }));
          } else {
            // This is a provider's movie list
            setProviderMovies(prev => ({
              ...prev,
              [response.source]: response.data
            }));
          }
        } else {
          console.warn(`Error from ${response.source}: ${response.message}`);
        }
      } catch (err) {
        console.error('Failed to parse SSE data', err);
        setError('Failed to parse streaming data');
      }
    };
    
    eventSource.onerror = (err) => {
      console.error('SSE Error:', err);
      setError('Connection error with movie stream');
      setLoading(false);
      eventSource.close();
    };
    
    eventSource.onopen = () => {
      setLoading(false);
    };
    
    return () => {
      eventSource.close();
    };
  }, []);
  
  return { providerMovies, movieDetails, priceComparisons, loading, error };
}; 