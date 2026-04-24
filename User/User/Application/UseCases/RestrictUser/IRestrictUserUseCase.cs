namespace Application.UseCases.RestrictUser;

public interface IRestrictUserUseCase
{
    Task<RestrictUserResponse> ExecuteAsync(RestrictUserCommand command);
}
