namespace Application.UseCases.UpdatePassword;

public record UpdatePasswordCommand(
    string Id,
    string CurrentPassword,
    string NewPassword);
