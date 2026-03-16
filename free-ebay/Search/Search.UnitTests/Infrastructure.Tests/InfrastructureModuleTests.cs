using Application.Gateways;
using Infrastructure;
using Infrastructure.AiSearch;
using Infrastructure.ElasticSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests;

[TestFixture]
public sealed class InfrastructureModuleTests
{
    [Test]
    public void AddInfrastructure_WhenElasticsearchUriMissing_ShouldThrow()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiSearch:Enabled"] = "false"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));

        Assert.That(ex!.Message, Does.Contain("Elasticsearch:Uri"));
    }

    [Test]
    public void AddInfrastructure_WhenAiDisabled_ShouldRegisterNullGateway()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = BuildConfiguration(aiEnabled: false);

        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        var aiGateway = provider.GetRequiredService<IAiSearchGateway>();
        var searcher = provider.GetRequiredService<IElasticsearchSearcher>();
        var initializer = provider.GetRequiredService<ElasticsearchIndexInitializer>();

        Assert.That(aiGateway, Is.TypeOf<NullAiSearchGateway>());
        Assert.That(searcher, Is.TypeOf<ElasticsearchSearcher>());
        Assert.That(initializer, Is.Not.Null);
    }

    [Test]
    public void AddInfrastructure_WhenAiEnabledWithoutGrpcUrl_ShouldThrow()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Elasticsearch:Uri"] = "http://localhost:9200",
                ["AiSearch:Enabled"] = "true"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));

        Assert.That(ex!.Message, Does.Contain("AiSearch:GrpcUrl"));
    }

    private static IConfiguration BuildConfiguration(bool aiEnabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Elasticsearch:Uri"] = "http://localhost:9200",
                ["AiSearch:Enabled"] = aiEnabled.ToString()
            })
            .Build();
    }
}
