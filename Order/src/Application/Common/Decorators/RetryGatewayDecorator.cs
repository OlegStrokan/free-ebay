using System.Net.Sockets;
using System.Reflection;
using Application.Common.Attributes;
using Application.Gateways.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Common.Decorators;

public class RetryGatewayDecorator<TGateway> : DispatchProxy
{
    private TGateway _decorated = default!;
    private ILogger _logger = default!;

    public static TGateway Create(TGateway decorated, ILogger logger)
    {
        var proxy = Create<TGateway, RetryGatewayDecorator<TGateway>>() as RetryGatewayDecorator<TGateway>;

        proxy!._decorated = decorated;
        proxy._logger = logger;

        return (TGateway)(object)proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            throw new ArgumentNullException(nameof(targetMethod));

        var retryAttr = targetMethod.GetCustomAttribute<RetryAttribute>();

        if (retryAttr == null)
            return targetMethod.Invoke(_decorated, args);

        return InvokeWithRetry(targetMethod, args, retryAttr);
    }

    private object? InvokeWithRetry(MethodInfo method, object?[]? args, RetryAttribute retryAttr)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= retryAttr.MaxRetries)
        {
            try
            {
                var result = method.Invoke(_decorated, args);

                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();

                    if (task.GetType().IsGenericType)
                    {
                        var resultProperty = task.GetType().GetProperty("Result");
                        return resultProperty?.GetValue(task);
                    }

                    return null;
                }

                return result;
            }
            catch (TargetInvocationException ex) when (
                IsTransientException(ex.InnerException) && attempt < retryAttr.MaxRetries)
            {
                lastException = ex.InnerException;
                attempt++;

                var delay = CalculateDelay(attempt, retryAttr);

                _logger.LogWarning(ex.InnerException,
                    "Transient failure in {Method}, attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms...",
                    method.Name, attempt, retryAttr.MaxRetries, delay);
                
                Thread.Sleep(delay);
            }
        }
        
        _logger.LogError(
            lastException,
            "Failed after {Attemps} attemps: {Method}",
            attempt, method.Name);

        throw lastException!;
    }

    private bool IsTransientException(Exception? ex)
    {
        if (ex == null)
            return false;

        if (ex is PaymentDeclinedException
            or InsufficientFundsException
            or InsufficientInventoryException
            or InvalidAddressException)
        {
            return false;
        }

        return ex is HttpRequestException
            or TaskCanceledException
            or TimeoutException
            or SocketException
            or IOException;
    }

    private int CalculateDelay(int attempt, RetryAttribute retryAttr)
    {
        if (!retryAttr.ExponentialBackoff)
            return retryAttr.DelayMilliseconds;

        // exponential backoff
        var baseDelay = retryAttr.DelayMilliseconds + (int)Math.Pow(2, attempt - 1);

        // randomness to prevent thundering herd
        var jitter = Random.Shared.Next(0, 100);

        return baseDelay + jitter;
    }
}