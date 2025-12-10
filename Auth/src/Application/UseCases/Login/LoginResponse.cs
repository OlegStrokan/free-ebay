namespace Application.UseCases.Login;

public record LoginResponse (
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType);