using MediatR;

namespace Application.Common;

public interface ICommand<TResponse> : IRequest<TResponse>;