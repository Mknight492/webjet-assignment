using System;

namespace MoviePriceComparison.Configuration;

public class ResilienceOptions
{
    public int MaxParallelism { get; set; } = 3;
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 200;
    public double RetryBackoffFactor { get; set; } = 2.0;
} 