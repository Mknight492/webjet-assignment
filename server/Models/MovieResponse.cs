namespace MoviePriceComparison.Models;

// This class represents the response from the /movies endpoint
public class MovieResponse
{
    public List<MovieItem> Movies { get; set; } = new List<MovieItem>();
}

// This is a single movie item in the movies list
public class MovieItem
{
    public string Title { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string ID { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Poster { get; set; } = string.Empty;
} 