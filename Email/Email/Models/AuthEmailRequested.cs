namespace Email.Models;

public sealed record AuthEmailMessage(
    Guid MessageId,
    string To,
    string From,
    string Subject,
    string HtmlBody,
    bool IsImportant,
    DateTime RequestedAtUtc);
