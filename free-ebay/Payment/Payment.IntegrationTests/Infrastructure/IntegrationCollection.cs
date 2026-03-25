using Xunit;

namespace Payment.IntegrationTests.Infrastructure;

[CollectionDefinition("PaymentIntegration")]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
}
