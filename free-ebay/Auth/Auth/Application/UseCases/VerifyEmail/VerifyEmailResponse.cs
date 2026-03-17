namespace Application.UseCases.VerifyEmail;

public record VerifyEmailResponse(bool Success, string Message, string? UserId);