import axios from "axios";

// Create an axios instance
const api = axios.create({
  baseURL: process.env.REACT_APP_API_URL || "https://localhost:7168/api",
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

// Add a response interceptor for error handling
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Handle errors globally
    console.error("API Error:", error);
    return Promise.reject(error);
  }
);

export default api;
