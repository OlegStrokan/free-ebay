
namespace Application.Common;

public interface ICommand<out TResponse> :IRequest<TResponse>
{
}