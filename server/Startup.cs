using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoviePriceComparison.Configuration;
using MoviePriceComparison.Services;

namespace MoviePriceComparison.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Register ResilienceOptions from configuration
            services.Configure<ResilienceOptions>(Configuration.GetSection("Resilience"));

            // Register the ResilienceService
            services.AddSingleton<IResilienceService, ResilienceService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Configure middleware here if needed
        }
    }
}