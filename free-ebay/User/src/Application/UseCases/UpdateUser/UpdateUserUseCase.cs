using Domain.Common.Interfaces;
using Domain.Repositories;

namespace Application.UseCases.UpdateUser;

public class UpdateUserUseCase (IUserRepository repository) : IUpdateUserUseCase
{

    public async Task<UpdateUserResponse> ExecuteAsync(UpdateUserCommand command)
    {
        var existingUser = await repository.GetUserById(command.Id);

        if (existingUser == null)
        {
            throw new KeyNotFoundException($"User with ID {command.Id} not found");
        }

        existingUser.Email = command.Email;
        existingUser.Fullname = command.Fullname;
        existingUser.Phone = command.Phone;
        // @todo: should be handled in db layer
        existingUser.UpdatedAt = DateTime.UtcNow;

        var user = await repository.UpdateUser(existingUser);
        
        return new UpdateUserResponse(user.Id, user.Email, user.Fullname, user.Phone, user.Status);
    }
}