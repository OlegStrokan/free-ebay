using Application.Gateways;
using Elastic.Clients.Elasticsearch;
using Infrastructure.AiSearch;
using Infrastructure.ElasticSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Protos.AiSearch;

namespace Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.AddElasticsearch(configuration);
        services.AddAiSearch(configuration);

        services.AddScoped<IElasticsearchSearcher, ElasticsearchSearcher>();
        services.AddSingleton<ElasticsearchIndexInitializer>();

        return services;
    }
    
    private static IServiceCollection AddElasticsearch(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        var uri = configuration["Elasticsearch:Uri"]
            ?? throw new InvalidOperationException(
                "Elasticsearch:Uri is not configured.");

        services.AddSingleton(_ => new ElasticsearchClient(new Uri(uri)));
        return services;
    }

    // -------------------------------------------------------------------------
    // Phase 0: AiSearch:Enabled => false => NullAiSearchGateway (stub)
    // Phase 3: AiSearch:Enabled =>true => AiSearchGateway  (real gRPC)
    // -------------------------------------------------------------------------
    private static IServiceCollection AddAiSearch(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        var enabled = configuration.GetValue<bool>("AiSearch:Enabled");

        if (!enabled)
        {
            services.AddScoped<IAiSearchGateway, NullAiSearchGateway>();
            return services;
        }

        var grpcUrl = configuration["AiSearch:GrpcUrl"]
            ?? throw new InvalidOperationException(
                "AiSearch:GrpcUrl is not configured.");

        services.AddGrpcClient<AiSearchService.AiSearchServiceClient>(o =>
        {
            o.Address = new Uri(grpcUrl);
        })
        .ConfigureChannel(o =>
        {
            o.HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                EnableMultipleHttp2Connections = true
            };
        });

        services.AddScoped<IAiSearchGateway, AiSearchGateway>();
        services.AddScoped<IAiSearchStreamGateway, AiSearchStreamGateway>();
        return services;
    }
}