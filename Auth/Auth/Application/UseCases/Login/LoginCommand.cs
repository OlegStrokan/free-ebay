namespace Application.UseCases.Login;

public record LoginCommand (
    string Email,
    string Password);