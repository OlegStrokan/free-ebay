using System.Net.Sockets;
using Infrastructure.BackgroundServices;
using Npgsql;

namespace Infrastructure.Tests.BackgroundServices;

public class ReadModelFailureClassifierTests
{
    // ── Systemic ─────────────────────────────────────────────────────────────

    [Fact]
    public void Classify_ShouldReturnSystemic_ForNpgsqlException()
    {
        var ex = new NpgsqlException("connection refused");
        Assert.Equal(ReadModelFailureKind.Systemic, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnSystemic_ForSocketException()
    {
        var ex = new SocketException((int)SocketError.TimedOut);
        Assert.Equal(ReadModelFailureKind.Systemic, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnSystemic_ForTimeoutException()
    {
        var ex = new TimeoutException("query timed out");
        Assert.Equal(ReadModelFailureKind.Systemic, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnSystemic_ForTaskCanceledWithTimeoutInner()
    {
        var ex = new TaskCanceledException("timeout", new TimeoutException());
        Assert.Equal(ReadModelFailureKind.Systemic, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnSystemic_WhenSystemicExceptionIsWrapped()
    {
        // A message-specific outer with a systemic inner => Systemic wins (infrastructure is the root cause)
        var ex = new InvalidOperationException("handler failed",
            new NpgsqlException("db unreachable"));
        Assert.Equal(ReadModelFailureKind.Systemic, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnSystemic_ForNestedTimeoutDeepInChain()
    {
        var ex = new AggregateException(
            new InvalidOperationException("wrapper",
                new InvalidOperationException("middle",
                    new TimeoutException("deep timeout"))));
        // AggregateException.InnerException is the first child
        Assert.Equal(ReadModelFailureKind.Systemic, ReadModelFailureClassifier.Classify(ex));
    }

    // ── MessageSpecific ───────────────────────────────────────────────────────

    [Fact]
    public void Classify_ShouldReturnMessageSpecific_ForInvalidOperationException()
    {
        var ex = new InvalidOperationException("domain rule violated");
        Assert.Equal(ReadModelFailureKind.MessageSpecific, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnMessageSpecific_ForJsonException()
    {
        var ex = new System.Text.Json.JsonException("bad payload");
        Assert.Equal(ReadModelFailureKind.MessageSpecific, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnMessageSpecific_ForKeyNotFoundException()
    {
        var ex = new KeyNotFoundException("aggregate not found");
        Assert.Equal(ReadModelFailureKind.MessageSpecific, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnMessageSpecific_ForTaskCanceledWithoutTimeoutInner()
    {
        // TaskCanceledException without TimeoutException inner is NOT Systemic
        var ex = new TaskCanceledException("cancelled normally");
        Assert.Equal(ReadModelFailureKind.MessageSpecific, ReadModelFailureClassifier.Classify(ex));
    }

    [Fact]
    public void Classify_ShouldReturnMessageSpecific_ForNullReferenceException()
    {
        var ex = new NullReferenceException("null reference in handler");
        Assert.Equal(ReadModelFailureKind.MessageSpecific, ReadModelFailureClassifier.Classify(ex));
    }
}
