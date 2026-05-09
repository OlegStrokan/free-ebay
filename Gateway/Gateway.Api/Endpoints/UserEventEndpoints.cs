using System.Security.Claims;
using Gateway.Api.Contracts.UserEvents;
using Gateway.Api.Services;

namespace Gateway.Api.Endpoints;

public static class UserEventEndpoints
{
    public static RouteGroupBuilder MapUserEventEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/user-events")
            .WithTags("UserEvents")
            .RequireAuthorization();

        group.MapPost("/view", async (
            ProductViewedRequest request,
            IUserEventPublisher publisher,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var payload = new
            {
                user_id = userId,
                catalog_item_id = request.CatalogItemId,
                duration_ms = request.DurationMs,
                source = request.Source,
                category = request.Category,
                brand = request.Brand,
                price = request.Price,
                condition = request.Condition,
            };

            await publisher.PublishAsync(userId, "ProductViewed", payload, ct);
            return Results.Accepted();
        });

        group.MapPost("/click", async (
            ProductClickedRequest request,
            IUserEventPublisher publisher,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var payload = new
            {
                user_id = userId,
                catalog_item_id = request.CatalogItemId,
                query_text = request.QueryText,
                rank = request.Rank,
                category = request.Category,
                brand = request.Brand,
                price = request.Price,
                condition = request.Condition,
            };

            await publisher.PublishAsync(userId, "ProductClicked", payload, ct);
            return Results.Accepted();
        });

        group.MapPost("/purchase", async (
            PurchaseCompletedRequest request,
            IUserEventPublisher publisher,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var payload = new
            {
                user_id = userId,
                catalog_item_id = request.CatalogItemId,
                listing_id = request.ListingId,
                price = request.Price,
                category = request.Category,
                brand = request.Brand,
                condition = request.Condition,
            };

            await publisher.PublishAsync(userId, "PurchaseCompleted", payload, ct);
            return Results.Accepted();
        });

        group.MapPost("/search-bounce", async (
            SearchBouncedRequest request,
            IUserEventPublisher publisher,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Results.Unauthorized();

            var payload = new
            {
                user_id = userId,
                query_text = request.QueryText,
            };

            await publisher.PublishAsync(userId, "SearchBounced", payload, ct);
            return Results.Accepted();
        });

        return group;
    }
}
