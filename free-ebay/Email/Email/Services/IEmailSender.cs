namespace Email.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string from, string subject, string htmlBody, CancellationToken cancellationToken);
}