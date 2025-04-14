import { useState, useEffect } from 'react';
import { Movie, MovieDetail, ServiceResponse } from '../types';
import { toCamelCase } from '../utils/casing';

export const useStreamingMovies2 = () => {
  const [providerMovies, setProviderMovies] = useState<Record<string, Movie[]>>({});
  const [movieDetails, setMovieDetails] = useState<Record<string, MovieDetail>>({});
  const [priceComparisons, setPriceComparisons] = useState<Movie[][]>([]);
  const [loading, setLoading] = useState({ movies: true, details: true });
  const [error, setError] = useState<string | null>(null);
  const [providerErrors, setProviderErrors] = useState<Record<string, string>>({});
  const [hasPartialData, setHasPartialData] = useState(false);

  // Track if both connections have been established
  const isLoading = loading.movies || loading.details;

  useEffect(() => {
    const baseUrl = process.env.REACT_APP_API_URL || 'https://localhost:7168';
    const movieEventSource = new EventSource(`${baseUrl}/api/Movies/stream/movies`);
    const detailsEventSource = new EventSource(`${baseUrl}/api/Movies/stream/details`);
    
    let moviesConnected = false;
    let detailsConnected = false;
    
    // Movies stream handling
    movieEventSource.onmessage = (event) => {
      try {
        const rawResponse = JSON.parse(event.data);
        const response = toCamelCase<ServiceResponse<any>>(rawResponse);
        
        if (response.success && response.data) {
          // Set hasPartialData to true as soon as we get any successful data
          setHasPartialData(true);
          
          // This is a provider's movie list
          setProviderMovies(prev => ({
            ...prev,
            [response?.source ?? '']: response.data
          }));
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
        console.error('Failed to parse movie SSE data', err);
        if (!hasPartialData) {
          setError('Failed to parse streaming movie data');
        }
      }
    };
    
    // Details stream handling
    detailsEventSource.onmessage = (event) => {
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
        console.error('Failed to parse details SSE data', err);
        if (!hasPartialData) {
          setError('Failed to parse streaming details data');
        }
      }
    };
    
    // Movie stream error handling
    movieEventSource.onerror = (err) => {
      console.error('Movies SSE Error:', err);
      
      // Only set global error if we have no data at all
      if (!hasPartialData && !detailsConnected) {
        setError('Connection error with movie stream');
      } else {
        // Otherwise just log the error but keep showing partial data
        console.warn('Movies connection issue, but showing partial data', err);
      }
      
      setLoading(prev => ({ ...prev, movies: false }));
      movieEventSource.close();
    };
    
    // Details stream error handling
    detailsEventSource.onerror = (err) => {
      console.error('Details SSE Error:', err);
      
      // Only set global error if we have no data at all
      if (!hasPartialData && !moviesConnected) {
        setError('Connection error with details stream');
      } else {
        // Otherwise just log the error but keep showing partial data
        console.warn('Details connection issue, but showing partial data', err);
      }
      
      setLoading(prev => ({ ...prev, details: false }));
      detailsEventSource.close();
    };
    
    // Connection established handlers
    movieEventSource.onopen = () => {
      moviesConnected = true;
      setLoading(prev => ({ ...prev, movies: false }));
    };
    
    detailsEventSource.onopen = () => {
      detailsConnected = true;
      setLoading(prev => ({ ...prev, details: false }));
    };
    
    return () => {
      movieEventSource.close();
      detailsEventSource.close();
    };
  }, [hasPartialData]);
  
  return { 
    providerMovies, 
    movieDetails, 
    priceComparisons, 
    loading: isLoading, 
    error,
    providerErrors,
    hasPartialData
  };
}; 