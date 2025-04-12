using Microsoft.AspNetCore.Mvc;
using MoviePriceComparison.Models;

namespace MoviePriceComparison.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(ILogger<MoviesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<List<Movie>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovies()
    {
        _logger.LogInformation("Getting all movies");
        
        // Placeholder - will be implemented with service calls
        var response = ServiceResponse<List<Movie>>.FromSuccess(new List<Movie>
        {
            new Movie { Id = "placeholder", Title = "Initial Setup - Movies will be fetched from services" }
        });
        
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<MovieDetails>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMovieDetails(string id)
    {
        _logger.LogInformation("Getting movie details for ID: {Id}", id);
        
        // Placeholder - will be implemented with service calls
        var response = ServiceResponse<MovieDetails>.FromSuccess(new MovieDetails 
        {
            Id = id,
            Title = "Initial Setup - Movie details will be fetched from services" 
        });
        
        return Ok(response);
    }
} 