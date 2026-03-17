namespace Application.UseCases.BlockUser;

public interface IBlockUserUseCase
{
    Task<BlockUserResponse> ExecuteAsync(BlockUserCommand command);
}
