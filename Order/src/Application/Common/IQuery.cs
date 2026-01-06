using MediatR;

namespace Application.Common;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}