namespace Gateway.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
            .WithTags("Health")
            .ExcludeFromDescription();

        routes.MapGet("/health/ready", () => Results.Ok(new { status = "Ready" }))
            .WithTags("Health")
            .ExcludeFromDescription();

        return routes;
    }
}
