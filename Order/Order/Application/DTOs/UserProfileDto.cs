namespace Application.DTOs;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string FullName,
    string CountryCode,
    string CustomerTier,
    bool IsActive);
