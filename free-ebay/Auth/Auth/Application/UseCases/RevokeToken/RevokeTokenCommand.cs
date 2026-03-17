namespace Application.UseCases.RevokeToken;

public record RevokeTokenCommand(string RefreshToken, string? RevokedById = null);