
using Application.Common.Interfaces;

namespace Infrastructure.Helpers;

public class BcryptPasswordHasher : IPasswordHasher
{
    //@think: it's probably too high
    private const int WorkFactor = 12;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be null or empty", nameof(passwordHash));
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        catch (BCrypt.Net.SaltParseException)
        {
            // invalid hash format
            return false;
        }
    }
}