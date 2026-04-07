using System.Net;
using System.Net.Mail;
using Email.Models;
using Email.Options;
using Microsoft.Extensions.Options;

namespace Email.Services;

public sealed class SmtpEmailSender(
    IOptions<EmailDeliveryOptions> emailOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender, IDisposable
{
    private readonly EmailDeliveryOptions _options = emailOptions.Value;
    private readonly SmtpClient _client = new SmtpClient(emailOptions.Value.SmtpHost, emailOptions.Value.SmtpPort)
    {
        EnableSsl = emailOptions.Value.EnableSsl,
        Credentials = new NetworkCredential(emailOptions.Value.Username, emailOptions.Value.Password)
    };

    public async Task SendAsync(OrderConfirmationEmailRequested request, CancellationToken cancellationToken)
    {
        var from = string.IsNullOrWhiteSpace(request.From)
            ? _options.DefaultFromAddress
            : request.From;

        using var message = new MailMessage(from, request.To, request.Subject, request.HtmlBody)
        {
            IsBodyHtml = true
        };

        await _client.SendMailAsync(message, cancellationToken);

        logger.LogInformation(
            "Email sent for Order {OrderId} to {Recipient}",
            request.OrderId,
            request.To);
    }

    public void Dispose() => _client.Dispose();
}