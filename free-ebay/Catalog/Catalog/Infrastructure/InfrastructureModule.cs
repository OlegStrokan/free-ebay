using Application.RetryStore;
using Application.Services;
using Elastic.Clients.Elasticsearch;
using Infrastructure.Elasticsearch;
using Infrastructure.Kafka;
using Infrastructure.RetryStore;
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
        services.Configure<RetryStoreOptions>(configuration.GetSection(RetryStoreOptions.SectionName));

        services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            var settings = new ElasticsearchClientSettings(new Uri(opts.Url))
                .DefaultIndex(opts.IndexName);
            return new ElasticsearchClient(settings);
        });

        services.AddScoped<IElasticsearchIndexer, ElasticsearchIndexer>();
        services.AddScoped<IRetryStore, PostgresRetryStore>();

        // Hosted services — startup order matters
        services.AddHostedService<ElasticsearchIndexInitializer>();
        services.AddHostedService<RetryStoreInitializer>();
        services.AddHostedService<KafkaConsumerBackgroundService>();
        services.AddHostedService<RetryWorkerBackgroundService>();

        return services;
    }
}
