using Domain.Entities.User;

namespace Application.UseCases.GetUserByEmail;

public record GetUserByEmailResponse(
    string Id,
    string Email,
    string Fullname,
    string Phone,
    string CountryCode,
    CustomerTier CustomerTier,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsEmailVerified,
    string PasswordHash);
