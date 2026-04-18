using Application.Dtos;
using Domain.Entities.User;
using Domain.Entities.UserRestriction;

namespace Application.UseCases.RestrictUser;

public record RestrictUserResponse(
    string Id,
    string Email,
    string Fullname,
    string Phone,
    string CountryCode,
    CustomerTier CustomerTier,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RestrictedById,
    RestrictionType RestrictionType,
    string Reason,
    DateTime? ExpiresAt,
    bool IsEmailVerified = false,
    IReadOnlyList<DeliveryInfoDto>? DeliveryInfos = null,
    IReadOnlyList<string>? Roles = null);
