using Microsoft.Extensions.DependencyInjection;
using Application.Queries.GetSimilarItems;
using Application.Queries.SearchProducts;
using Domain.Common.Interfaces;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<
            IQueryHandler<SearchProductsQuery, SearchProductsResult>,
            SearchProductsQueryHandler>();

        services.AddScoped<
            IQueryHandler<GetSimilarItemsQuery, GetSimilarItemsResult>,
            GetSimilarItemsQueryHandler>();

        return services;
    }
}