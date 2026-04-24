using Infrastructure.Persistence;
using Npgsql;

namespace Infrastructure.Tests.Persistence;

// @think: not sure if it's make sense, but for now i leave it
public class PostgresSagaErrorClassifierTests
{
    private readonly PostgresSagaErrorClassifier _sut = new();
    private sealed class TransientNpgsqlException : NpgsqlException
    {
        public TransientNpgsqlException() : base("transient connectivity error") { }
        public override bool IsTransient => true;
    }

    private sealed class NonTransientNpgsqlException : NpgsqlException
    {
        public NonTransientNpgsqlException() : base("syntax error") { }        public override bool IsTransient => false;
    }

    [Fact]
    public void IsTransient_ShouldReturnTrue_WhenNpgsqlExceptionIsTransient()
    {
        Assert.True(_sut.IsTransient(new TransientNpgsqlException()));
    }

    [Fact]
    public void IsTransient_ShouldReturnFalse_WhenNpgsqlExceptionIsNotTransient()
    {
        Assert.False(_sut.IsTransient(new NonTransientNpgsqlException()));
    }

    [Fact]
    public void IsTransient_ShouldReturnTrue_WhenTimeoutException()
    {
        Assert.True(_sut.IsTransient(new TimeoutException("db timeout")));
    }

    [Fact]
    public void IsTransient_ShouldReturnTrue_WhenHttpRequestException()
    {
        Assert.True(_sut.IsTransient(new HttpRequestException("connection refused")));
    }

    [Fact]
    public void IsTransient_ShouldReturnFalse_WhenGenericException()
    {
        Assert.False(_sut.IsTransient(new InvalidOperationException("unexpected state")));
    }

    [Fact]
    public void IsTransient_ShouldReturnFalse_WhenArgumentException()
    {
        Assert.False(_sut.IsTransient(new ArgumentException("bad argument")));
    }
}
