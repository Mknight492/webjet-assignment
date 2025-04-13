namespace MoviePriceComparison.Configuration;

public class CacheOptions
{
    // Default cache duration for movie lists
    public TimeSpan MoviesCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
    
    // Default cache duration for movie details
    public TimeSpan MovieDetailsCacheDuration { get; set; } = TimeSpan.FromMinutes(10);
} 