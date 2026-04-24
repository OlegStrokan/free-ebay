using System.Net.Sockets;
using Infrastructure.Elasticsearch;
using Infrastructure.Kafka;

namespace Infrastructure.Tests.Kafka;

[TestFixture]
public class FailureClassifierTests
{
    #region Systemic failures

    [Test]
    public void Classify_HttpRequestException_ReturnsSystemic()
    {
        var ex = new HttpRequestException("Connection refused");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [Test]
    public void Classify_SocketException_ReturnsSystemic()
    {
        var ex = new SocketException((int)SocketError.ConnectionRefused);

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [Test]
    public void Classify_TimeoutException_ReturnsSystemic()
    {
        var ex = new TimeoutException("The operation timed out");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [Test]
    public void Classify_TaskCanceledWithTimeoutInner_ReturnsSystemic()
    {
        var inner = new TimeoutException("timed out");
        var ex = new TaskCanceledException("timed out", inner);

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [TestCase("Elasticsearch connection refused on node xyz")]
    [TestCase("Request timeout talking to Elasticsearch")]
    [TestCase("Elasticsearch cluster unreachable")]
    [TestCase("Failed to upsert product abc in index 'products': 503")]
    [TestCase("Failed to upsert product abc in index 'products': 502")]
    [TestCase("Failed to upsert product abc in index 'products': 429")]
    [TestCase("Elasticsearch cluster unavailable")]
    public void Classify_ElasticsearchIndexingException_WithSystemicMessage_ReturnsSystemic(string message)
    {
        var ex = new ElasticsearchIndexingException(message);

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [Test]
    public void Classify_InnerExceptionIsSystemic_ReturnsSystemic()
    {
        var inner = new SocketException((int)SocketError.HostNotFound);
        var ex = new InvalidOperationException("wrapper", inner);

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [Test]
    public void Classify_DeepNestedSystemicException_ReturnsSystemic()
    {
        var deepInner = new HttpRequestException("network unreachable");
        var middle = new InvalidOperationException("middle", deepInner);
        var outer = new Exception("outer", middle);

        Assert.That(FailureClassifier.Classify(outer), Is.EqualTo(FailureKind.Systemic));
    }

    #endregion

    #region Message-specific failures

    [Test]
    public void Classify_ElasticsearchIndexingException_WithMappingError_ReturnsMessageSpecific()
    {
        var ex = new ElasticsearchIndexingException(
            "Failed to upsert product abc in index 'products': mapper_parsing_exception");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.MessageSpecific));
    }

    [Test]
    public void Classify_ElasticsearchIndexingException_WithGenericError_ReturnsMessageSpecific()
    {
        var ex = new ElasticsearchIndexingException(
            "Failed to update fields for product abc in index 'products': unknown Elasticsearch error");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.MessageSpecific));
    }

    [Test]
    public void Classify_ElasticsearchIndexingException_WithVersionConflict_ReturnsMessageSpecific()
    {
        var ex = new ElasticsearchIndexingException(
            "Failed to upsert product abc in index 'products': version_conflict_engine_exception");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.MessageSpecific));
    }

    #endregion

    #region Edge cases

    [Test]
    public void Classify_UnknownException_ReturnsSystemic()
    {
        // Unknown exceptions default to systemic to avoid data loss
        var ex = new InvalidOperationException("something unexpected");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [Test]
    public void Classify_ArgumentException_ReturnsSystemic()
    {
        // Not ElasticsearchIndexingException, not a known systemic type → defaults to Systemic
        var ex = new ArgumentException("bad arg");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    [Test]
    public void Classify_TaskCanceledWithoutTimeoutInner_ReturnsSystemic()
    {
        // TaskCanceledException without TimeoutException inner → unknown → systemic default
        var ex = new TaskCanceledException("cancelled");

        Assert.That(FailureClassifier.Classify(ex), Is.EqualTo(FailureKind.Systemic));
    }

    #endregion
}
