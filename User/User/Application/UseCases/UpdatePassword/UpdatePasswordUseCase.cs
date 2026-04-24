using Domain.Common.Interfaces;
using Domain.Repositories;

namespace Application.UseCases.UpdatePassword;

public class UpdatePasswordUseCase(
    IUserRepository repository,
    IPasswordHasher passwordHasher) : IUpdatePasswordUseCase
{
    public async Task ExecuteAsync(UpdatePasswordCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            throw new ArgumentException("User id is required", nameof(command.Id));
        }

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            throw new ArgumentException("Current password is required", nameof(command.CurrentPassword));
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < 8)
        {
            throw new ArgumentException("New password must be at least 8 characters long", nameof(command.NewPassword));
        }

        var user = await repository.GetUserById(command.Id);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {command.Id} not found");
        }

        if (!passwordHasher.VerifyPassword(command.CurrentPassword, user.Password))
        {
            throw new InvalidOperationException("Current password is invalid");
        }

        user.Password = passwordHasher.HashPassword(command.NewPassword);
        await repository.UpdateUser(user);
    }
}
