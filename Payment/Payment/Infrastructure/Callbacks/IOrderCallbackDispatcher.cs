using Domain.Entities;

namespace Infrastructure.Callbacks;

public interface IOrderCallbackDispatcher
{
    Task<CallbackDeliveryResult> DispatchAsync(
        OutboundOrderCallback callback,
        CancellationToken cancellationToken = default);
}