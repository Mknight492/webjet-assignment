namespace MoviePriceComparison.Configuration;

public class MovieApiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
    public int RetryCount { get; set; } = 3;
    public int CacheDurationMinutes { get; set; } = 10;
} 