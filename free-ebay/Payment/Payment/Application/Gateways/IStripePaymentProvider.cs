using Application.Gateways.Models;

namespace Application.Gateways;

public interface IStripePaymentProvider
{
    Task<ProcessPaymentProviderResult> ProcessPaymentAsync(
        ProcessPaymentProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<RefundPaymentProviderResult> RefundPaymentAsync(
        RefundPaymentProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<ProviderPaymentStatusResult> GetPaymentStatusAsync(
        string providerPaymentIntentId,
        CancellationToken cancellationToken = default);

    Task<ProviderRefundStatusResult> GetRefundStatusAsync(
        string providerRefundId,
        CancellationToken cancellationToken = default);
}