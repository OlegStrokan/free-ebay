using System.Collections.Generic;
using Application.Dtos;
using Domain.Entities.User;

namespace Application.UseCases.BlockUser;

public record BlockUserResponse(
    string Id,
    string Email,
    string Fullname,
    string Phone,
    string CountryCode,
    CustomerTier CustomerTier,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsEmailVerified = false,
    IReadOnlyList<DeliveryInfoDto>? DeliveryInfos = null);
