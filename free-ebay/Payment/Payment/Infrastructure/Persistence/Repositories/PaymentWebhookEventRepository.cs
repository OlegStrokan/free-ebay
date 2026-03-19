using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class PaymentWebhookEventRepository(PaymentDbContext dbContext) : IPaymentWebhookEventRepository
{
    public async Task<PaymentWebhookEvent?> GetByProviderEventIdAsync(
        string providerEventId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PaymentWebhookEvents
            .FirstOrDefaultAsync(x => x.ProviderEventId == providerEventId, cancellationToken);
    }

    public async Task AddAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.PaymentWebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }

    public Task UpdateAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Entry(webhookEvent);
        if (entry.State == EntityState.Detached)
        {
            dbContext.PaymentWebhookEvents.Update(webhookEvent);
        }

        return Task.CompletedTask;
    }
}