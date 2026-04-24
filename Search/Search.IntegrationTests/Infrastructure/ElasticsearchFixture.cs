using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;
using Xunit;

namespace Search.IntegrationTests.Infrastructure;

[CollectionDefinition("Elasticsearch")]
public sealed class ElasticsearchCollection : ICollectionFixture<ElasticsearchFixture>;

public sealed class ElasticsearchFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder()
        .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.13.4")
        .Build();

    public ElasticsearchClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var connectionUri = new Uri(connectionString);
        var userInfo = connectionUri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "elastic";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "elastic";

        var settings = new ElasticsearchClientSettings(connectionUri)
            .Authentication(new BasicAuthentication(username, password))
            .ServerCertificateValidationCallback((_, _, _, _) => true);

        Client = new ElasticsearchClient(settings);

        // Container startup can complete before the cluster is fully ready.
        var ready = false;
        for (var attempt = 0; attempt < 60; attempt++)
        {
            try
            {
                var ping = await Client.PingAsync();
                if (ping.IsValidResponse)
                {
                    ready = true;
                    break;
                }
            }
            catch
            {
                // Ignore startup race and retry.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        if (!ready)
            throw new InvalidOperationException("Elasticsearch test container did not become ready in time.");
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
