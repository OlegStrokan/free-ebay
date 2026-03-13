using Application.Services;
using Elastic.Clients.Elasticsearch;
using Infrastructure.Elasticsearch;
using Infrastructure.Kafka;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));

        services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            var settings = new ElasticsearchClientSettings(new Uri(opts.Url))
                .DefaultIndex(opts.IndexName);
            return new ElasticsearchClient(settings);
        });

        services.AddScoped<IElasticsearchIndexer, ElasticsearchIndexer>();
        services.AddHostedService<ElasticsearchIndexInitializer>();
        services.AddHostedService<KafkaConsumerBackgroundService>();

        return services;
    }
}
