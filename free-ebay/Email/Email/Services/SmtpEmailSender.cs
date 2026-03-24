using System.Net;
using System.Net.Mail;
using Email.Models;
using Email.Options;
using Microsoft.Extensions.Options;

namespace Email.Services;

public sealed class SmtpEmailSender(
    IOptions<EmailDeliveryOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailDeliveryOptions _options = emailOptions.Value;

    public async Task SendAsync(OrderConfirmationEmailRequested request, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };

        var from = string.IsNullOrWhiteSpace(request.From)
            ? _options.DefaultFromAddress
            : request.From;

        using var message = new MailMessage(from, request.To, request.Subject, request.HtmlBody)
        {
            IsBodyHtml = true
        };

        await client.SendMailAsync(message, cancellationToken);

        logger.LogInformation(
            "Email sent for Order {OrderId} to {Recipient}",
            request.OrderId,
            request.To);
    }
}