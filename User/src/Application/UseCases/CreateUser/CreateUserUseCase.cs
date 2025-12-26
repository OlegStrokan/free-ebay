using Domain.Common.Interfaces;
using Domain.Entities.User;
using Domain.Repositories;

namespace Application.UseCases.CreateUser;

public class CreateUserUseCase(IUserRepository userRepository, IIdGenerator idGenerator) : ICreateUserUseCase
{
    
    public async Task<CreateUserResponse> ExecuteAsync(CreateUserCommand command)
    {
        var userEntity = new UserEntity
        {
            Id = idGenerator.GenerateId(),
            Email = command.Email,
            Password = command.Password,
            Fullname =  command.Fullname,
            Phone = command.Phone,
            CreatedAt =  DateTime.UtcNow,
        };
        var user = await userRepository.CreateUser(userEntity);

        return new CreateUserResponse(user.Id, user.Email, user.Fullname, user.Status);
    }
}