using System.Text;
using System.Text.Json;
using Application.DTOs;
using Application.Gateways;
using Application.Gateways.Exceptions;
using Application.Interfaces;
using Infrastructure.Persistence.DbContext;

namespace Infrastructure.Gateways;


public class EmailGateway
(IConfiguration configuration,
    IUserGateway userGateway,
    IOutboxRepository outboxRepository,
    AppDbContext dbContext,
    ILogger<EmailGateway> logger) : IEmailGateway
{

    public async Task SendOrderConfirmationAsync(
        Guid customerId,
        Guid orderId,
        decimal orderTotal,
        string currency,
        List<OrderItemDto> items,
        AddressDto deliveryDelivery,
        DateTime estimatedDelivery,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Preparing order confirmation email request for Order {OrderId} and Customer {CustomerId}. " +
            "Total: {Total} {Currency}. Estimated delivery: {EstimatedDelivery}",
            orderId, customerId, orderTotal, currency, estimatedDelivery);

        var fromAddress = configuration["Email:FromAddress"] ?? "no-reply@free-ebay.com";

        var subject = $"Order Confirmation #{orderId}";
        var body = BuildOrderConfirmationBody(orderId, orderTotal, currency, items, deliveryDelivery,
            estimatedDelivery);

        string recipientEmail;
        try
        {
            var customerProfile = await userGateway.GetUserProfileAsync(customerId, cancellationToken);
            recipientEmail = customerProfile.Email;
        }
        catch (CustomerNotFoundException ex)
        {
            logger.LogWarning(ex,
                "Cannot send order confirmation email for Order {OrderId}. Customer {CustomerId} not found.",
                orderId,
                customerId);
            return;
        }

        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            logger.LogWarning(
                "Cannot send order confirmation email for Order {OrderId}. Customer {CustomerId} has no email.",
                orderId,
                customerId);
            return;
        }

        var eventId = Guid.NewGuid();
        var payload = new OrderConfirmationEmailRequested(
            eventId,
            customerId,
            orderId,
            recipientEmail,
            fromAddress,
            subject,
            body,
            DateTime.UtcNow);

        try
        {
            await outboxRepository.AddAsync(
                eventId,
                nameof(OrderConfirmationEmailRequested),
                JsonSerializer.Serialize(payload),
                payload.RequestedAtUtc,
                orderId.ToString(),
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Enqueued {EventType} for Order {OrderId} in outbox for async Kafka delivery",
                nameof(OrderConfirmationEmailRequested),
                orderId);
        }

        catch (Exception ex)
        {
            // Notification dispatch must never fail the saga step.
            logger.LogError(ex,
                "Failed to enqueue order confirmation email request for Order {OrderId}. " +
                "Customer {CustomerId} will not receive email notification.",
                orderId,
                customerId);
        }
    }

    private sealed record OrderConfirmationEmailRequested(
        Guid MessageId,
        Guid CustomerId,
        Guid OrderId,
        string To,
        string From,
        string Subject,
        string HtmlBody,
        DateTime RequestedAtUtc);

    private static string BuildOrderConfirmationBody(
        Guid orderId,
        decimal orderTotal,
        string currency,
        List<OrderItemDto> items,
        AddressDto deliveryAddress,
        DateTime estimatedDelivery)
    {
        var sb = new StringBuilder();

        sb.Append($"<h2> Your order <strong>#{orderId}</strong> has been confirmed!</h2>");
        sb.Append("<h3> Items ordered:</h3><ul>");

        foreach (var item in items)
        {
            sb.Append($"<li>{item.ProductId} + {item.Quantity} - {item.Price} {item.Currency}</li>");
        }

        sb.Append("</ul>");
        sb.Append($"<p><strong>Total:</strong> {orderTotal} {currency}</p>");
        sb.Append($"<p><strong>Delivery address:</strong> {deliveryAddress.Street}, " +
                      $"{deliveryAddress.City}, {deliveryAddress.Country} {deliveryAddress.PostalCode}</p>");
        sb.Append($"<p><strong>Estimated delivery:</strong> {estimatedDelivery:D}</p>");

        return sb.ToString(); 
        
    }
}