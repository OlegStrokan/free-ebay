using Domain.Entities.User;

namespace Application.UseCases.CreateUser;

public record CreateUserResponse(
	string Id,
	string Email,
	string Fullname,
	string Phone,
	string CountryCode,
	CustomerTier CustomerTier,
	UserStatus Status,
	DateTime CreatedAt,
	DateTime UpdatedAt,
	bool IsEmailVerified = false);