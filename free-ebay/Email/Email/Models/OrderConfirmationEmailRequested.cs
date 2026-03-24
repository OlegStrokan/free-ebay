namespace Email.Models;

public sealed record OrderConfirmationEmailRequested(
    Guid MessageId,
    Guid CustomerId,
    Guid OrderId,
    bool IsImportant,
    string To,
    string From,
    string Subject,
    string HtmlBody,
    DateTime RequestedAtUtc);