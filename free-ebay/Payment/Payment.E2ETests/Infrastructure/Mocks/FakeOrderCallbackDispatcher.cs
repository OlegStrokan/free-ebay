using Domain.Entities;
using Infrastructure.Callbacks;

namespace Payment.E2ETests.Infrastructure.Mocks;

public sealed class FakeOrderCallbackDispatcher : IOrderCallbackDispatcher
{
    public List<OutboundOrderCallback> Calls { get; } = [];

    public bool ShouldSucceed { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public Task<CallbackDeliveryResult> DispatchAsync(
        OutboundOrderCallback callback,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(callback);

        if (ShouldSucceed)
        {
            return Task.FromResult(new CallbackDeliveryResult(true, null));
        }

        return Task.FromResult(new CallbackDeliveryResult(false, ErrorMessage ?? "dispatch failed"));
    }

    public void Reset()
    {
        Calls.Clear();
        ShouldSucceed = true;
        ErrorMessage = null;
    }
}
