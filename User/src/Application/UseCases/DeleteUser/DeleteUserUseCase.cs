using Domain.Repositories;

namespace Application.UseCases.DeleteUser;

public class DeleteUserUseCase(IUserRepository repository) : IDeleteUserUseCase
{
    public async Task ExecuteAsync(string id)
    {
        var existingUser = await repository.GetUserById(id);

        if (existingUser == null) return; // idempotent delete: it's its gone, it's gone

        await repository.DeleteUser(id);

    }
}