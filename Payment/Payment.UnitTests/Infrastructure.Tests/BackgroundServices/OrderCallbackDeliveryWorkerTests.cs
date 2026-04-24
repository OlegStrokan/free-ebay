using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Infrastructure.BackgroundServices;
using Infrastructure.Callbacks;
using Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Reflection;

namespace Infrastructure.Tests.BackgroundServices;

public class OrderCallbackDeliveryWorkerTests
{
    private static readonly DateTime FixedNow = new(2026, 3, 24, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ProcessBatchAsync_ShouldMarkDeliveredAndSave_WhenDispatchSucceeds()
    {
        var callbackRepository = Substitute.For<IOutboundOrderCallbackRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var dispatcher = Substitute.For<IOrderCallbackDispatcher>();

        clock.UtcNow.Returns(FixedNow);

        var callback = OutboundOrderCallback.Create("evt-1", "order-1", "PaymentSucceededEvent", "{\"ok\":true}", FixedNow.AddMinutes(-10));
        callbackRepository.GetPendingAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([callback]);
        dispatcher.DispatchAsync(callback, Arg.Any<CancellationToken>())
            .Returns(new CallbackDeliveryResult(true, null));

        var services = new ServiceCollection();
        services.AddSingleton(callbackRepository);
        services.AddSingleton(unitOfWork);
        services.AddSingleton(clock);
        services.AddSingleton(dispatcher);
        services.AddSingleton<IOutboundOrderCallbackRepository>(callbackRepository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<IOrderCallbackDispatcher>(dispatcher);

        var provider = services.BuildServiceProvider();

        var worker = new OrderCallbackDeliveryWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new OrderCallbackOptions
            {
                BatchSize = 10,
                MaxAttempts = 8,
                BaseRetryDelaySeconds = 5,
                MaxRetryDelaySeconds = 300,
                PollIntervalSeconds = 1,
            }),
            NullLogger<OrderCallbackDeliveryWorker>.Instance);

        var method = typeof(OrderCallbackDeliveryWorker)
            .GetMethod("ProcessBatchAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        var task = (Task<int>)method!.Invoke(worker, [CancellationToken.None])!;
        var processed = await task;

        Assert.Equal(1, processed);
        Assert.Equal(CallbackDeliveryStatus.Delivered, callback.Status);
        Assert.Equal(1, callback.AttemptCount);

        await callbackRepository.Received(1).UpdateAsync(callback, Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_ShouldMarkPermanentFailure_WhenMaxAttemptsReached()
    {
        var callbackRepository = Substitute.For<IOutboundOrderCallbackRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<IClock>();
        var dispatcher = Substitute.For<IOrderCallbackDispatcher>();

        clock.UtcNow.Returns(FixedNow);

        var callback = OutboundOrderCallback.Create("evt-2", "order-2", "PaymentFailedEvent", "{\"ok\":false}", FixedNow.AddMinutes(-10));
        callbackRepository.GetPendingAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([callback]);
        dispatcher.DispatchAsync(callback, Arg.Any<CancellationToken>())
            .Returns(new CallbackDeliveryResult(false, "HTTP 500"));

        var services = new ServiceCollection();
        services.AddSingleton(callbackRepository);
        services.AddSingleton(unitOfWork);
        services.AddSingleton(clock);
        services.AddSingleton(dispatcher);
        services.AddSingleton<IOutboundOrderCallbackRepository>(callbackRepository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IClock>(clock);
        services.AddSingleton<IOrderCallbackDispatcher>(dispatcher);

        var provider = services.BuildServiceProvider();

        var worker = new OrderCallbackDeliveryWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new OrderCallbackOptions
            {
                BatchSize = 10,
                MaxAttempts = 1,
                BaseRetryDelaySeconds = 5,
                MaxRetryDelaySeconds = 300,
                PollIntervalSeconds = 1,
            }),
            NullLogger<OrderCallbackDeliveryWorker>.Instance);

        var method = typeof(OrderCallbackDeliveryWorker)
            .GetMethod("ProcessBatchAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);
        var task = (Task<int>)method!.Invoke(worker, [CancellationToken.None])!;
        var processed = await task;

        Assert.Equal(1, processed);
        Assert.Equal(CallbackDeliveryStatus.PermanentFailure, callback.Status);
        Assert.Equal("HTTP 500", callback.LastError);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
