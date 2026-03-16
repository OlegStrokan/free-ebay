using Domain.Entities;

namespace Domain.Interfaces;

public interface IPaymentWebhookEventRepository
{
    Task<PaymentWebhookEvent?> GetByProviderEventIdAsync(string providerEventId, CancellationToken cancellationToken = default);

    Task AddAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    Task UpdateAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}