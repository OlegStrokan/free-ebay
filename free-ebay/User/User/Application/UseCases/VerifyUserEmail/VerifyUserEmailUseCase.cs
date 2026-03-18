using Domain.Repositories;

namespace Application.UseCases.VerifyUserEmail;

public class VerifyUserEmailUseCase(IUserRepository repository) : IVerifyUserEmailUseCase
{
    public async Task<bool> ExecuteAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required", nameof(userId));
        }

        var user = await repository.GetUserById(userId);
        if (user == null)
        {
            return false;
        }

        if (user.IsEmailVerified)
        {
            return true;
        }

        user.IsEmailVerified = true;
        await repository.UpdateUser(user);

        return true;
    }
}
