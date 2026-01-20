namespace Application.UseCases.GetUserById;

public interface IGetUserByIdUseCase
{
    Task<GetUserByIdResponse?> ExecuteAsync(string command);
}