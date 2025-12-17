namespace Application.UseCases.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword);