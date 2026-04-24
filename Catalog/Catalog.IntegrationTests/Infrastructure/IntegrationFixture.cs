using Application.Models;
using Application.Services;
using Elastic.Clients.Elasticsearch;
using Infrastructure.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Elasticsearch;

namespace Catalog.IntegrationTests.Infrastructure;

public abstract class IntegrationFixture
{
    private ElasticsearchContainer _elasticsearch = null!;
    private ServiceProvider _services = null!;

    protected IServiceProvider Services => _services;

    protected ElasticsearchClient Client { get; private set; } = null!;

    protected const string IndexName = "products-integration-test";

    [OneTimeSetUp]
    public async Task FixtureSetUpAsync()
    {
        _elasticsearch = new ElasticsearchBuilder().Build();
        await _elasticsearch.StartAsync();

        var url = _elasticsearch.GetConnectionString();

        // AllowAll bypasses the self-signed cert Testcontainers.Elasticsearch uses in ES 8
        Client = new ElasticsearchClient(
            new ElasticsearchClientSettings(new Uri(url))
                .ServerCertificateValidationCallback((_, _, _, _) => true));

        var services = new ServiceCollection();

        services.AddLogging(b => b
            .AddConsole()
            .SetMinimumLevel(LogLevel.Warning));

        services.Configure<ElasticsearchOptions>(opt =>
        {
            opt.Url = url;
            opt.IndexName = IndexName;
        });

        services.AddSingleton(Client);
        services.AddScoped<IElasticsearchIndexer, ElasticsearchIndexer>();

        _services = services.BuildServiceProvider();

        var opts = _services.GetRequiredService<IOptions<ElasticsearchOptions>>();
        var initializer = new ElasticsearchIndexInitializer(
            Client, opts, NullLogger<ElasticsearchIndexInitializer>.Instance);

        await initializer.StartAsync(CancellationToken.None);
    }

    [OneTimeTearDown]
    public async Task FixtureTearDownAsync()
    {
        await _services.DisposeAsync();
        await _elasticsearch.DisposeAsync();
    }

    protected IServiceScope CreateScope() => _services.CreateScope();
    
    protected async Task<GetResponse<ProductSearchDocument>> GetDocumentAsync(string id)
    {
        //refresh async forces all pending buffered writes to be flushed and made searchable right now, before the assertion runs
        await Client.Indices.RefreshAsync(r => r.Indices(IndexName));
        return await Client.GetAsync<ProductSearchDocument>(id, g => g.Index(IndexName));
    }
}
