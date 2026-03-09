using FluentValidation;
using MediatR;

namespace Application.Common;

internal sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var errors = failures.Select(f => f.ErrorMessage).ToList();
        var responseType = typeof(TResponse);

        // Non-generic Result (void commands)
        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(string.Join("; ", errors));

        // Generic Result<T>
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(nameof(Result.Failure), [typeof(List<string>)]);
            if (failureMethod is not null)
                return (TResponse)failureMethod.Invoke(null, [errors])!;
        }

        throw new ValidationException(failures);
    }
}
