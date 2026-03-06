using Application.Sagas;
using Application.Sagas.Persistence;
using FluentAssertions;
using Infrastructure.BackgroundServices;
using Infrastructure.Persistence.DbContext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Order.IntegrationTests.Infrastructure;
using Xunit;

namespace Order.IntegrationTests.Sagas;

[Collection("Integration")]
public sealed class SagaWatchdogTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;

    public SagaWatchdogTests(IntegrationFixture fixture) => _fixture = fixture;
    
    [Fact]
    public async Task SagaWatchdog_ShouldMarkStuckSaga_AsFailed_WhenBeyondDoubleThreshold()
    {
        // seed a Running saga whose last update was 11 minutes ago
        // (stuckThreshold × 2 = 5 min × 2 = 10 min => triggers FailAndCompensateSagaAsync)
        var sagaId = Guid.NewGuid();

        await using (var setupScope = _fixture.CreateScope())
        {
            var repo = setupScope.ServiceProvider.GetRequiredService<ISagaRepository>();

            var saga = new SagaState
            {
                Id            = sagaId,
                CorrelationId = Guid.NewGuid(),
                SagaType      = $"OrderSaga-{sagaId}", 
                CurrentStep   = "PaymentStep",
                Status        = SagaStatus.Running,
                Context       = "{}",
                Payload       = "{}",
                CreatedAt     = DateTime.UtcNow.AddMinutes(-15),
                UpdatedAt     = DateTime.UtcNow.AddMinutes(-11)  // beyond the 10-min double threshold
            };

            await repo.SaveAsync(saga, CancellationToken.None);
        }

        // start the watchdog; the first poll fires before the 1-minute Task.Delay
        var watchdog = new SagaWatchdogService(
            _fixture.Services,
            NullLogger<SagaWatchdogService>.Instance);

        var cts = new CancellationTokenSource();
        await watchdog.StartAsync(cts.Token);
        await Task.Delay(600, cts.Token);  // allow the first CheckAndRecoverStuckSagaAsync to complete
        await cts.CancelAsync();
        await watchdog.StopAsync(CancellationToken.None);

        // reload in a fresh scope to bypass EF first-level cache
        await using var assertScope = _fixture.CreateScope();
        var assertRepo = assertScope.ServiceProvider.GetRequiredService<ISagaRepository>();

        var loaded = await assertRepo.GetByIdAsync(sagaId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(SagaStatus.Failed,
            "the watchdog must transition a saga stuck beyond twice the threshold to Failed");
    }
}
