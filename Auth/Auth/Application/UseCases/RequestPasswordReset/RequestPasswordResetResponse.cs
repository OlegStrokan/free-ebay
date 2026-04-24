namespace Application.UseCases.RequestPasswordReset;

public record RequestPasswordResetResponse(
    bool Success,
    string Message,
    string? ResetToken );