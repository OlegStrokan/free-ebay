using Application.Dtos;
using Domain.Repositories;
using System.Net.Mail;

namespace Application.UseCases.GetUserByEmail;

public class GetUserByEmailUseCase(IUserRepository repository) : IGetUserByEmailUseCase
{
    public async Task<GetUserByEmailResponse?> ExecuteAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required", nameof(email));
        }

        try
        {
            _ = new MailAddress(email.Trim());
        }
        catch (FormatException)
        {
            throw new ArgumentException("Email format is invalid", nameof(email));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await repository.GetUserByEmail(normalizedEmail);

        if (user == null)
        {
            return null;
        }

        return new GetUserByEmailResponse(
            user.Id,
            user.Email,
            user.Fullname,
            user.Phone,
            user.CountryCode,
            user.CustomerTier,
            user.Status,
            user.CreatedAt,
            user.UpdatedAt,
            user.IsEmailVerified,
            user.DeliveryInfos.ToDtos(),
            user.UserRoles.Select(ur => ur.Role.Name).ToList());
    }
}
