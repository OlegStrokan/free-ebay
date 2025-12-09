namespace Application.Common.Interfaces;

public interface IJwtTokenValidator
{
    TokenValidationResult ValidateToken(string token);
}

public class TokenValidationResult
{
    public bool isValid { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
}