using Application.Queries.SearchProducts;
using Infrastructure.AiSearch;

namespace Infrastructure.Tests.AiSearch;

[TestFixture]
public sealed class NullAiSearchGatewayTests
{
    [Test]
    public void SearchAsync_ShouldThrowNotSupportedException()
    {
        var gateway = new NullAiSearchGateway();
        var query = new SearchProductsQuery("query", UseAi: true, Page: 1, Size: 10);

        var ex = Assert.ThrowsAsync<NotSupportedException>(() => gateway.SearchAsync(query, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("AI search is disabled"));
    }
}
