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
    
    // Additional properties from MovieDetails
    public string? Plot { get; set; }
    public string? Rated { get; set; }
    public string? Released { get; set; }
    public string? Runtime { get; set; }
    public string? Genre { get; set; }
    public string? Director { get; set; }
    public string? Writer { get; set; }
    public string? Actors { get; set; }
    public decimal? Price { get; set; }
    
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