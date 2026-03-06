using Domain.Repositories;

namespace Application.UseCases.GetUserById;

public class GetUserByIdUseCase(IUserRepository repository) : IGetUserByIdUseCase
{
    public async Task<GetUserByIdResponse?> ExecuteAsync(string id)
    {
        var existingUser = await repository.GetUserById(id);

        if (existingUser == null)
        {
            return null;
        }
        
        return new GetUserByIdResponse(
            existingUser.Id, existingUser.Email, existingUser.Fullname, existingUser.Phone, existingUser.Status);
    }
}