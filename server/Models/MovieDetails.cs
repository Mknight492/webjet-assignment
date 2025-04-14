namespace MoviePriceComparison.Models;

// This is our domain model for movie details, used in our application
public class MovieDetails : Movie
{
    public decimal Price { get; set; }
    public string Rating { get; set; } = string.Empty;
    public string Plot { get; set; } = string.Empty;
    public string Actors { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Director { get; set; } = string.Empty;
    public string Runtime { get; set; } = string.Empty;
    
    // Create MovieDetails from a MovieDetailsResponse
    public static MovieDetails FromMovieDetailsResponse(MovieDetailsResponse response, string provider)
    {
        return new MovieDetails
        {
            Id = response.ID,
            Title = response.Title,
            Year = int.TryParse(response.Year, out int year) ? year : 0,
            Type = response.Type,
            Poster = response.Poster,
            Provider = provider,
            Price = response.GetPriceAsDecimal(),
            Rating = response.Rating,
            Plot = response.Plot,
            Actors = response.Actors,
            Genre = response.Genre,
            Director = response.Director,
            Runtime = response.Runtime
        };
    }
} 