using Domain.Exceptions;

namespace Domain.ValueObjects;

public sealed record FailureReason
{
    private const int MaxCodeLength = 64;
    private const int MaxMessageLength = 1024;

    public string? Code { get; init; }

    public string Message { get; init; }

    public FailureReason(string? code, string message)
    {
        if (!string.IsNullOrWhiteSpace(code) && code.Trim().Length > MaxCodeLength)
        {
            throw new InvalidValueException($"Failure code cannot exceed {MaxCodeLength} characters");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidValueException("Failure message cannot be empty");
        }

        var normalizedMessage = message.Trim();
        if (normalizedMessage.Length > MaxMessageLength)
        {
            throw new InvalidValueException($"Failure message cannot exceed {MaxMessageLength} characters");
        }

        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        Message = normalizedMessage;
    }

    public static FailureReason Create(string? code, string message) => new(code, message);

    public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Message : $"{Code}: {Message}";
}