import { useQuery } from "@tanstack/react-query";
import api from "../utils/api";
import { Movie } from "../types/Movie";


interface MoviesResponse {
  Data: Movie[];
}

export const useMovies = () => {
  return useQuery<MoviesResponse, Error>({
    queryKey: ["movies"],
    queryFn: async () => {
        const response = await api.get<MoviesResponse>("/movies");
        console.log(response.data);
        return response.data;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
};

export default useMovies; 