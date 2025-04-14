namespace MoviePriceComparison.Models;

public class ServiceResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool FromCache { get; set; } = false;

    public static ServiceResponse<T> FromSuccess(T data, string source = "", bool fromCache = false)
    {
        return new ServiceResponse<T>
        {
            Success = true,
            Data = data,
            Source = source,
            FromCache = fromCache
        };
    }

    public static ServiceResponse<T> FromError(string message, string source = "")
    {
        return new ServiceResponse<T>
        {
            Success = false,
            Message = message,
            Source = source
        };
    }
} 