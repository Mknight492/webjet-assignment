namespace MoviePriceComparison.Configuration;

public class CacheOptions
{
    public const string Position = "Cache";
    
    // Default cache duration for movie lists
    public TimeSpan MoviesCacheDuration { get; set; } = TimeSpan.FromMinutes(15);
    
    // Default cache duration for movie details
    public TimeSpan MovieDetailsCacheDuration { get; set; } = TimeSpan.FromMinutes(30);
    
    // Default cache duration for movie posters
    public TimeSpan PosterCacheDuration { get; set; } = TimeSpan.FromHours(24);
} 