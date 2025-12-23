
namespace Application.UseCases.VerifyEmail;

public interface IVerifyEmailUseCase 
{
    Task<VerifyEmailResponse> ExecuteAsync(VerifyEmailCommand command);
}
