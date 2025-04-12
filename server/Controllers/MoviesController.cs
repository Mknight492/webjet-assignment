using Microsoft.AspNetCore.Mvc;
using MoviePriceComparison.Models;
using MoviePriceComparison.Services.Interfaces;

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

    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<List<Movie>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMovies()
    {
        _logger.LogInformation("Getting all movies");
        
        var response = await _movieAggregator.GetAllMoviesAsync();
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<MovieDetails>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMovieDetails(string id)
    {
        _logger.LogInformation("Getting movie details for ID: {Id}", id);
        
        var response = await _movieAggregator.GetMovieDetailsAsync(id);
        
        if (!response.Success)
        {
            return NotFound(response);
        }
        
        return Ok(response);
    }
} 