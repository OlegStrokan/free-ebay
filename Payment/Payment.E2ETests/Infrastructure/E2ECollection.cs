using Xunit;

namespace Payment.E2ETests.Infrastructure;

[CollectionDefinition("PaymentE2E")]
public sealed class E2ECollection : ICollectionFixture<E2ETestServer>
{
}
