using Api.Middleware;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Api.Tests.Middleware;

[TestFixture]
public sealed class ExceptionHandlingInterceptorTests
{
    private ExceptionHandlingInterceptor _sut = null!;
    private ServerCallContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        var logger = Substitute.For<ILogger<ExceptionHandlingInterceptor>>();
        _sut = new ExceptionHandlingInterceptor(logger);
        _context = Substitute.For<ServerCallContext>();
    }

    [Test]
    public async Task UnaryServerHandler_WhenContinuationSucceeds_ShouldReturnResponse()
    {
        var response = await _sut.UnaryServerHandler<string, string>(
            request: "req",
            _context,
            (_, _) => Task.FromResult("ok"));

        Assert.That(response, Is.EqualTo("ok"));
    }

    [Test]
    public void UnaryServerHandler_WhenArgumentExceptionThrown_ShouldMapToInvalidArgument()
    {
        var ex = Assert.ThrowsAsync<RpcException>(() => _sut.UnaryServerHandler<string, string>(
            request: "req",
            _context,
            (_, _) => throw new ArgumentException("bad input")));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
        Assert.That(ex.Status.Detail, Is.EqualTo("bad input"));
    }

    [Test]
    public void UnaryServerHandler_WhenUnexpectedExceptionThrown_ShouldMapToInternal()
    {
        var ex = Assert.ThrowsAsync<RpcException>(() => _sut.UnaryServerHandler<string, string>(
            request: "req",
            _context,
            (_, _) => throw new InvalidOperationException("boom")));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Internal));
        Assert.That(ex.Status.Detail, Is.EqualTo("An internal error occurred."));
    }

    [Test]
    public void UnaryServerHandler_WhenRpcExceptionThrown_ShouldRethrowAsIs()
    {
        var original = new RpcException(new Status(StatusCode.Unavailable, "upstream down"));

        var ex = Assert.ThrowsAsync<RpcException>(() => _sut.UnaryServerHandler<string, string>(
            request: "req",
            _context,
            (_, _) => throw original));

        Assert.That(ex!.StatusCode, Is.EqualTo(StatusCode.Unavailable));
        Assert.That(ex.Status.Detail, Is.EqualTo("upstream down"));
    }
}
