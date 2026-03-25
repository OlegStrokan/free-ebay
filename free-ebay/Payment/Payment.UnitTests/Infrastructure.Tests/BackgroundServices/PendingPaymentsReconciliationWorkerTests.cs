using Application.Commands.ReconcilePendingPayments;
using Application.Common;
using Application.DTOs;
using Infrastructure.BackgroundServices;
using Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Reflection;

namespace Infrastructure.Tests.BackgroundServices;

public class PendingPaymentsReconciliationWorkerTests
{
    [Fact]
    public async Task RunReconciliationAsync_ShouldSendCommandWithNormalizedDefaults()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<ReconcilePendingPaymentsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<ReconciliationResultDto>.Success(new ReconciliationResultDto(0, 0, 0, 0, 0, 0, 0)));

        var services = new ServiceCollection();
        services.AddSingleton(mediator);
        var provider = services.BuildServiceProvider();

        var worker = new PendingPaymentsReconciliationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new ReconciliationWorkerOptions
            {
                Enabled = true,
                IntervalSeconds = 1,
                OlderThanMinutes = 0,
                BatchSize = 0,
            }),
            NullLogger<PendingPaymentsReconciliationWorker>.Instance);

        var method = typeof(PendingPaymentsReconciliationWorker)
            .GetMethod("RunReconciliationAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        var task = (Task)method!.Invoke(worker, [CancellationToken.None])!;
        await task;

        await mediator.Received(1).Send(
            Arg.Is<ReconcilePendingPaymentsCommand>(c => c.OlderThanMinutes == 15 && c.BatchSize == 100),
            Arg.Any<CancellationToken>());
    }
}
