namespace MoviePriceComparison.Models;

// This is our domain model for a movie, used in our application
public class Movie
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Poster { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty; // "Cinemaworld" or "Filmworld"
    
    // Create a Movie from a MovieItem response
    public static Movie FromMovieItem(MovieItem item, string provider)
    {
        return new Movie
        {
            Id = item.ID,
            Title = item.Title,
            Year = int.TryParse(item.Year, out int year) ? year : 0,
            Type = item.Type,
            Poster = item.Poster,
            Provider = provider
        };
    }
} 