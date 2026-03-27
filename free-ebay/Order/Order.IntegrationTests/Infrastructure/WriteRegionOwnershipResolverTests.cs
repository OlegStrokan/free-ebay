using Application.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.Infrastructure;

[Collection("Integration")]
public sealed class WriteRegionOwnershipResolverTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public WriteRegionOwnershipResolverTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ResolveForCustomer_ShouldBeDeterministic_ForSameCustomer()
    {
        await using var scope = _fixture.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IWriteRegionOwnershipResolver>();

        var customerId = Guid.Parse("a4072443-dad8-4f40-a8aa-a4d66df8f02d");

        var first = resolver.ResolveForCustomer(customerId);
        var second = resolver.ResolveForCustomer(customerId);

        first.IsEnabled.Should().BeTrue();
        first.OwnerRegion.Should().Be(second.OwnerRegion);
        first.IsCurrentRegionOwner.Should().Be(second.IsCurrentRegionOwner);
    }

    [Fact]
    public async Task ResolveForCustomer_ShouldDistributeAcrossConfiguredRegions()
    {
        await using var scope = _fixture.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IWriteRegionOwnershipResolver>();

        var owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < 200; i++)
        {
            var decision = resolver.ResolveForCustomer(Guid.NewGuid());
            owners.Add(decision.OwnerRegion);
        }

        owners.Should().Contain("eu-west-1");
        owners.Should().Contain("us-east-1");
        owners.Should().Contain("ap-southeast-1");
    }
}
