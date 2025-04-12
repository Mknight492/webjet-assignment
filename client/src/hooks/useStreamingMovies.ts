import { useState, useEffect } from 'react';
import { Movie, MovieDetail, ServiceResponse } from '../types';

export const useStreamingMovies = () => {
  const [providerMovies, setProviderMovies] = useState<Record<string, Movie[]>>({});
  const [movieDetails, setMovieDetails] = useState<Record<string, MovieDetail>>({});
  const [priceComparisons, setPriceComparisons] = useState<Movie[][]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const eventSource = new EventSource(`${process.env.REACT_APP_API_URL || 'https://localhost:7168/api'}/Movies/stream`);
    
    eventSource.onmessage = (event) => {
      try {
        const response = JSON.parse(event.data);
        console.log(response);
        if (response.Success && response.Data) {
          if (response.Source === 'PriceComparison') {
            // This is a price comparison group
            setPriceComparisons(prev => [...prev, response.Data]);
          } else if (response.Source.endsWith('Details')) {
            // This is movie details from a provider
            const movieDetail = response.Data;
            const provider = response.Source.replace('Details', '');
            
            setMovieDetails(prev => ({
              ...prev,
              [`${provider}-${movieDetail.Id}`]: movieDetail
            }));
          } else {
            // This is a provider's movie list
            setProviderMovies(prev => ({
              ...prev,
              [response.Source]: response.Data
            }));
          }
        } else {
          console.warn(`Error from ${response.Source}: ${response.Message}`);
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