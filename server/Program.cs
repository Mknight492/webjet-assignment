using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using MoviePriceComparison.Configuration;

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

// Configure HTTP clients
builder.Services.AddHttpClient("Cinemaworld", client =>
{
    client.BaseAddress = new Uri("https://webjetapitest.azurewebsites.net/api/cinemaworld/");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["MovieApi:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("Filmworld", client =>
{
    client.BaseAddress = new Uri("https://webjetapitest.azurewebsites.net/api/filmworld/");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["MovieApi:ApiKey"]);
    client.Timeout = TimeSpan.FromSeconds(5);
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

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add caching
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpLogging();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("CorsPolicy");
app.UseResponseCaching();

app.UseAuthorization();

app.MapControllers();

app.Run();
