using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using MoviePriceComparison.Configuration;
using MoviePriceComparison.Services;
using MoviePriceComparison.Services.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// Add configuration for movie APIs
builder.Services.Configure<MovieApiOptions>(
    builder.Configuration.GetSection("MovieApi"));

// Add configuration for caching
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection("Cache"));

// Add memory cache
builder.Services.AddMemoryCache();

// By default, we want all HttpClient instances to include the StandardResilienceHandler.
builder.Services.ConfigureHttpClientDefaults(builder => builder.AddStandardResilienceHandler(options => {
  options.Retry = new HttpRetryStrategyOptions
    {
        // Customize and configure the retry logic.
        BackoffType = DelayBackoffType.Linear,
        MaxRetryAttempts = 10,
        UseJitter = true,
        Delay = TimeSpan.FromMilliseconds(100)
    };
}));    

// Configure HTTP clients
builder.Services.AddHttpClient("Cinemaworld", client =>
{
    client.BaseAddress = new Uri("https://webjetapitest.azurewebsites.net/api/cinemaworld/");
    client.DefaultRequestHeaders.Add("x-access-token", builder.Configuration["MovieApi:ApiKey"]);
});

builder.Services.AddHttpClient("Filmworld", client =>
{
    client.BaseAddress = new Uri("https://webjetapitest.azurewebsites.net/api/filmworld/");
    client.DefaultRequestHeaders.Add("x-access-token", builder.Configuration["MovieApi:ApiKey"]);
});

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyMethod()
               .AllowAnyHeader());
});

// Add HTTP logging for development
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders | 
                            HttpLoggingFields.ResponseStatusCode;
});

// Add OpenAPI/Swagger with enhanced configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Movie Price Comparison API",
        Version = "v1",
        Description = "A BFF API for movie price comparison between providers",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        }
    });
});

// Add caching
builder.Services.AddResponseCaching();

// Register the services
builder.Services.AddTransient<IMovieService, CinemaworldService>();
builder.Services.AddTransient<IMovieService, FilmworldService>();
builder.Services.AddTransient<IMovieAggregatorService, MovieAggregatorService>();

var app = builder.Build();

// Generate and save OpenAPI specification file
var openApiFilePath = Path.Combine(AppContext.BaseDirectory, "openapi.json");
using (var serviceScope = app.Services.CreateScope())
{
    var services = serviceScope.ServiceProvider;
    var swaggerProvider = services.GetRequiredService<ISwaggerProvider>();
    var swagger = swaggerProvider.GetSwagger("v1");

    using (var fileStream = File.Create(openApiFilePath))
    using (var textWriter = new StreamWriter(fileStream))
    {
        // OpenApiJsonWriter doesn't implement IDisposable, so don't use it in a using statement
        var jsonWriter = new OpenApiJsonWriter(textWriter);
        swagger.SerializeAsV3(jsonWriter);
    }

    app.Logger.LogInformation($"OpenAPI specification saved to: {openApiFilePath}");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpLogging();
}

if (!app.Environment.IsDevelopment())
{
    // app.UseHttpsRedirection();
}

app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseResponseCaching();

app.UseAuthorization();

app.MapControllers();

app.Run();
