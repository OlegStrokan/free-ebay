using System.Net;
using System.Net.Mail;
using System.Text;
using Application.DTOs;
using Application.Gateways;

namespace Infrastructure.Gateways;

// @think: should this service be a separate microservice?
// yes, change it to sending kafka events, and in separate email service
// listen for kafka messages.....i guess
public class EmailGateway
(IConfiguration configuration,
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
        // @todo: replace with real customer email lookup when user service will be ready
        logger.LogInformation(
            "Sending order confirmation for Order {OrderId} to Customer {CustomerId}. " +
            "Total: {Total} {Currency}. Estimated delivery: {EstimatedDelivery}",
            orderId, customerId, orderTotal, currency, estimatedDelivery);

        var smtpHost = configuration["Email:SmtpHost"];

        if (string.IsNullOrEmpty(smtpHost))
        {
            logger.LogWarning(
                "Email:SmtpHost is not configured. Order confirmation email for Order {OrderId} was not sent.",
                orderId);
            return;
        }
        

        var smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        var smtpUser = configuration["Email:Username"] ?? string.Empty;
        var smtpPassword = configuration["Email:Password"] ?? string.Empty;
        var fromAddress = configuration["Email:FromAddress"] ?? "no-reply@free-ebay.com";

        var subject = $"Order Confirmation #{orderId}";
        var body = BuildOrderConfirmationBody(orderId, orderTotal, currency, items, deliveryDelivery,
            estimatedDelivery);

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPassword),
            EnableSsl = true
        };
        
        // @todo: real implementation should resolve customer email via User service gRPC
        // placeholder recipient - swap with actual lookup
        var recipientEmail = $"{customerId}@placeholder.internal";

        var message = new MailMessage(fromAddress, recipientEmail, subject, body)
        {
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            logger.LogInformation("Order confirmation email send for Order {OrderId}", orderId);
        }

        catch (Exception ex)
        {
            // email failure must never fail the saga step, just log and move on
            logger.LogError(ex,
                "Failed to send order confirmation email for Order {OrderId}." +
                "Customer {CustomerId} will not receive email notification.", orderId, customerId);
        }
    }

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