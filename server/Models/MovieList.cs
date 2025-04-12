namespace MoviePriceComparison.Models;

public class MovieList
{
    public List<Movie> Movies { get; set; } = new List<Movie>();
    public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
    
    // Helper to track if we have data from a specific provider
    public bool HasDataFromProvider(string provider)
    {
        return Movies.Any(m => m.Provider == provider);
    }
} 