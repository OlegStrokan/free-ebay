using Email.Models;

namespace Email.Services;

public interface IEmailSender
{
    Task SendAsync(OrderConfirmationEmailRequested request, CancellationToken cancellationToken);
}