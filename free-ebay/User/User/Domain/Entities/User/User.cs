using System;

namespace Domain.Entities.User;

public enum UserStatus
{
    Active = 0,
    Blocked = 1,
}

public enum CustomerTier
{
    Standard = 0,
    Subscriber = 1,
    Premium = 2,
}

public class UserEntity
{
    public required string Id { get; init; }
    public required string Fullname { get; set; }
    public required string Password { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string CountryCode { get; set; }

    public CustomerTier CustomerTier { get; set; } = CustomerTier.Standard;
    public UserStatus Status { get; set; } = UserStatus.Active;

    public required DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}