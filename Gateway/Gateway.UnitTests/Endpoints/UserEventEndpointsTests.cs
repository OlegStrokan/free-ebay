using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Gateway.Api.Contracts.UserEvents;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Gateway.UnitTests.Endpoints;

public sealed class UserEventEndpointsTests : IClassFixture<UserEventEndpointsTests.Factory>
{
    private readonly Factory _factory;
    private readonly IUserEventPublisher _publisher;

    public UserEventEndpointsTests(Factory factory)
    {
        _factory = factory;
        _publisher = factory.Publisher;
    }

    [Fact]
    public async Task PostView_WithAuthUser_ShouldReturn202AndPublish()
    {
        using var client = _factory.CreateAuthenticatedClient("user-1");

        var response = await client.PostAsJsonAsync("/api/v1/user-events/view",
            new ProductViewedRequest("item-1", DurationMs: 5000, Source: "search", Category: "Electronics"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await _publisher.Received(1).PublishAsync(
            "user-1",
            "ProductViewed",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostClick_WithAuthUser_ShouldReturn202AndPublish()
    {
        using var client = _factory.CreateAuthenticatedClient("user-2");

        var response = await client.PostAsJsonAsync("/api/v1/user-events/click",
            new ProductClickedRequest("item-2", "headphones", Rank: 3));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await _publisher.Received().PublishAsync(
            "user-2",
            "ProductClicked",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostPurchase_WithAuthUser_ShouldReturn202AndPublish()
    {
        using var client = _factory.CreateAuthenticatedClient("user-3");

        var response = await client.PostAsJsonAsync("/api/v1/user-events/purchase",
            new PurchaseCompletedRequest("item-3", Price: 150.0));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await _publisher.Received().PublishAsync(
            "user-3",
            "PurchaseCompleted",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostSearchBounce_WithAuthUser_ShouldReturn202AndPublish()
    {
        using var client = _factory.CreateAuthenticatedClient("user-4");

        var response = await client.PostAsJsonAsync("/api/v1/user-events/search-bounce",
            new SearchBouncedRequest("nonexistent product"));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        await _publisher.Received().PublishAsync(
            "user-4",
            "SearchBounced",
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostView_WithoutAuth_ShouldReturn401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/user-events/view",
            new ProductViewedRequest("item-1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public IUserEventPublisher Publisher { get; } = Substitute.For<IUserEventPublisher>();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Replace the real Kafka publisher with a mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserEventPublisher));
                if (descriptor != null)
                    services.Remove(descriptor);
                services.AddSingleton(Publisher);

                // Add fake authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
            });
        }

        public HttpClient CreateAuthenticatedClient(string userId)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test");
            return client;
        }
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-UserId", out var userIdValues))
                return Task.FromResult(AuthenticateResult.Fail("No test user ID"));

            var userId = userIdValues.ToString();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, $"test-{userId}")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
