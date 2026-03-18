using Domain.Repositories;

namespace Application.UseCases.UpdateUserPassword;

public class UpdateUserPasswordUseCase(IUserRepository repository) : IUpdateUserPasswordUseCase
{
    public async Task<UpdateUserPasswordResult> ExecuteAsync(UpdateUserPasswordCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            throw new ArgumentException("User id is required", nameof(command.UserId));
        }

        if (string.IsNullOrWhiteSpace(command.NewPasswordHash))
        {
            throw new ArgumentException("New password hash is required", nameof(command.NewPasswordHash));
        }

        var user = await repository.GetUserById(command.UserId);
        if (user == null)
        {
            return new UpdateUserPasswordResult(false, $"User with ID {command.UserId} not found");
        }

        user.Password = command.NewPasswordHash;
        await repository.UpdateUser(user);

        return new UpdateUserPasswordResult(true, "Password updated successfully");
    }
}
