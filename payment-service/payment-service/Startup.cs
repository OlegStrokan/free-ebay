using payment_service.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = false;
            });        services.AddScoped<PaymentService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            // Map Controllers
            endpoints.MapControllers();

            // Log all routes for debugging purposes
            var routeEndpoints = endpoints.DataSources
                .SelectMany(ds => ds.Endpoints)
                .OfType<RouteEndpoint>();

            foreach (var endpoint in routeEndpoints)
            {
                logger.LogInformation($"Registered Route: {endpoint.RoutePattern.RawText}");
            }
        });
    }
}