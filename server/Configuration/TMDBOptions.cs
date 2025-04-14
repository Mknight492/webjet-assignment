namespace MoviePriceComparison.Configuration;

public class TMDBOptions
{
    public const string Position = "TMDB";
    
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.themoviedb.org/3/";
    public string ImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/";
}