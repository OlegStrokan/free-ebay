using FluentAssertions;
using Infrastructure.ElasticSearch;
using Microsoft.Extensions.Logging.Abstractions;
using Search.IntegrationTests.Infrastructure;
using Xunit;

namespace Search.IntegrationTests.ElasticSearch;

[Collection("Elasticsearch")]
public sealed class ElasticsearchIndexInitializerTests
{
    private readonly ElasticsearchFixture _fixture;

    public ElasticsearchIndexInitializerTests(ElasticsearchFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EnsureIndexAsync_WhenIndexMissing_ShouldCreateProductsIndex()
    {
        await DeleteProductsIndexIfExistsAsync();

        var initializer = new ElasticsearchIndexInitializer(
            _fixture.Client,
            NullLogger<ElasticsearchIndexInitializer>.Instance);

        await initializer.EnsureIndexAsync();

        var exists = await _fixture.Client.Indices.ExistsAsync(ElasticsearchIndexInitializer.IndexName);

        (exists.ApiCallDetails.HttpStatusCode == 200)
            .Should()
            .BeTrue("index bootstrap should create the products index when missing");
    }

    [Fact]
    public async Task EnsureIndexAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        await DeleteProductsIndexIfExistsAsync();

        var initializer = new ElasticsearchIndexInitializer(
            _fixture.Client,
            NullLogger<ElasticsearchIndexInitializer>.Instance);

        await initializer.EnsureIndexAsync();
        await initializer.EnsureIndexAsync();

        var exists = await _fixture.Client.Indices.ExistsAsync(ElasticsearchIndexInitializer.IndexName);

        (exists.ApiCallDetails.HttpStatusCode == 200).Should().BeTrue();
    }

    private async Task DeleteProductsIndexIfExistsAsync()
    {
        var exists = await _fixture.Client.Indices.ExistsAsync(ElasticsearchIndexInitializer.IndexName);

        if (exists.ApiCallDetails.HttpStatusCode == 200)
            await _fixture.Client.Indices.DeleteAsync(ElasticsearchIndexInitializer.IndexName);
    }
}
