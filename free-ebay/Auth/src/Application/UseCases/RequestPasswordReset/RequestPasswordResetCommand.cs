namespace Application.UseCases.RequestPasswordReset;

public record RequestPasswordResetCommand(
    string Email,
    string IpAddress = null );