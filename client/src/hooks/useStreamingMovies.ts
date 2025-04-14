import { useState, useEffect } from 'react';
import { Movie, MovieDetail, ServiceResponse } from '../types';
import { toCamelCase } from '../utils/casing';

export const useStreamingMovies = () => {
  const [providerMovies, setProviderMovies] = useState<Record<string, Movie[]>>({});
  const [movieDetails, setMovieDetails] = useState<Record<string, MovieDetail>>({});
  const [priceComparisons, setPriceComparisons] = useState<Movie[][]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [providerErrors, setProviderErrors] = useState<Record<string, string>>({});
  const [hasPartialData, setHasPartialData] = useState(false);

  useEffect(() => {
    const eventSource = new EventSource(`${process.env.REACT_APP_API_URL || 'https://localhost:7168'}/api/Movies/stream`);
    
    eventSource.onmessage = (event) => {
      try {
        const rawResponse = JSON.parse(event.data);
        const response = toCamelCase<ServiceResponse<any>>(rawResponse);
        
        if (response.success && response.data) {
          // Set hasPartialData to true as soon as we get any successful data
          setHasPartialData(true);
          
          if (response.source === 'PriceComparison') {
            // This is a price comparison group
            setPriceComparisons(prev => [...prev, response.data]);
          } else if (response?.source?.endsWith('Details')) {
            // This is movie details from a provider
            const movieDetail = response.data as MovieDetail;
            const provider = response.source.replace('Details', '');
            
            setMovieDetails(prev => ({
              ...prev,
              [`${provider}-${movieDetail.id}`]: movieDetail
            }));
          } else {
            // This is a provider's movie list
            setProviderMovies(prev => ({
              ...prev,
              [response?.source ?? '']: response.data
            }));
          }
        } else {
          // Track provider-specific errors
          if (response.source) {
            setProviderErrors(prev => ({
              ...prev,
              [response?.source ?? '']: response.message || 'Unknown error'
            }));
          }
          console.warn(`Error from ${response.source}: ${response.message}`);
        }
      } catch (err) {
        console.error('Failed to parse SSE data', err);
        setError('Failed to parse streaming data');
      }
    };
    
    eventSource.onerror = (err) => {
      console.error('SSE Error:', err);
      // Only set global error if we have no data at all
      if (!hasPartialData) {
        setError('Connection error with movie stream');
      } else {
        // Otherwise just log the error but keep showing partial data
        console.warn('Connection issue, but showing partial data', err);
      }
      setLoading(false);
      eventSource.close();
    };
    
    eventSource.onopen = () => {
      setLoading(false);
    };
    
    return () => {
      eventSource.close();
    };
  }, [hasPartialData]);
  
  return { 
    providerMovies, 
    movieDetails, 
    priceComparisons, 
    loading, 
    error,
    providerErrors,
    hasPartialData
  };
}; 