using Infrastructure.Options;

namespace Infrastructure.Tests.Options;

public class OrderCallbackOptionsTests
{
    [Fact]
    public void Defaults_ShouldMatchExpectedValues()
    {
        var options = new OrderCallbackOptions();

        Assert.Equal(10, options.TimeoutSeconds);
        Assert.Equal(5, options.PollIntervalSeconds);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(8, options.MaxAttempts);
        Assert.Equal(5, options.BaseRetryDelaySeconds);
        Assert.Equal(300, options.MaxRetryDelaySeconds);
        Assert.Equal(string.Empty, options.EndpointUrl);
        Assert.Equal(string.Empty, options.SharedSecret);
    }
}