using System.Net;
using System.Net.Mail;
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

    public async Task SendAsync(string to, string from, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var resolvedFrom = string.IsNullOrWhiteSpace(from) ? _options.DefaultFromAddress : from;

        using var message = new MailMessage(resolvedFrom, to, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        await _client.SendMailAsync(message, cancellationToken);

        logger.LogInformation("Email sent to {Recipient} with subject '{Subject}'", to, subject);
    }

    public void Dispose() => _client.Dispose();
}