using Microsoft.AspNetCore.Mvc;
using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace MoviePriceComparison.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieAggregatorService _movieAggregator;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(
        IMovieAggregatorService movieAggregator,
        ILogger<MoviesController> logger)
    {
        _movieAggregator = movieAggregator;
        _logger = logger;
    }

    [HttpGet("stream")]
    public async Task StreamMovies(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SSE stream for movies");
        
        // Set headers for SSE
        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        
        // Get the response stream
        var response = Response.Body;
        
        // Create a json serializer
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        try
        {
            // Stream movies and prices as they become available
            await foreach (var movieResponse in _movieAggregator.StreamMoviesAsync(cancellationToken))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                // Serialize the response to JSON - now it can be any type
                var json = JsonSerializer.Serialize(movieResponse, options);
                
                // Format as SSE event
                var sseEvent = $"data: {json}\n\n";
                var bytes = Encoding.UTF8.GetBytes(sseEvent);
                
                // Write to response stream
                await response.WriteAsync(bytes, cancellationToken);
                await response.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE stream was canceled by the client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while streaming movies");
            
            // Send error event
            var errorEvent = $"event: error\ndata: {ex.Message}\n\n";
            var errorBytes = Encoding.UTF8.GetBytes(errorEvent);
            await response.WriteAsync(errorBytes, cancellationToken);
            await response.FlushAsync(cancellationToken);
        }
    }
} 