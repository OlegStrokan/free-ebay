using System.Net.Sockets;
using System.Reflection;
using Application.Common.Attributes;
using Application.Gateways.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Common.Decorators;

// @think: too much voodoo. dispatch proxy is sync but so we spell a lot of magic 
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
        if (targetMethod == null) throw new ArgumentNullException(nameof(targetMethod));

        var retryAttr = targetMethod.GetCustomAttribute<RetryAttribute>();

        if (retryAttr == null)
            return targetMethod.Invoke(_decorated, args);
        
        // if it's not task
        if (!typeof(Task).IsAssignableFrom(targetMethod.ReturnType))
            return InvokeSyncWithRetry(targetMethod, args, retryAttr);
        
        
        // if it's task
        var returnType = targetMethod.ReturnType;
        var taskResultType = returnType.IsGenericType
            ? returnType.GetGenericArguments()[0]
            : typeof(object);

        var method = typeof(RetryGatewayDecorator<TGateway>)
            .GetMethod(nameof(InvokeAsyncWithRetryInternal), BindingFlags.NonPublic | BindingFlags.Instance)
            ?.MakeGenericMethod(taskResultType);

        return method?.Invoke(this, new object?[] { targetMethod, args, retryAttr });
    }

    private async Task<T?> InvokeAsyncWithRetryInternal<T>(MethodInfo method, object?[]? args,
        RetryAttribute? retryAttr)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                var result = method.Invoke(_decorated, args);
            
                // If the method returns task<T>, await it
                if (result is Task<T> task) return await task;
            
                // If the method returns a plain tak, await it and return default
                if (result is Task plainTask)
                {
                    await plainTask;
                    return default;
                }

                return (T?)result;
            }
            catch (TargetInvocationException ex) when (IsTransientException(ex.InnerException) && attempt < retryAttr?.MaxRetries)
            {
                attempt++;
                var delay = CalculateDelay(attempt, retryAttr!);
                _logger.LogWarning("Retry {Attempt} for {Method} after {Delay}ms", attempt, method.Name, delay);
            
                await Task.Delay(delay); // non-blocking 
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException ?? ex;
            }
        }
    }



    private object? InvokeSyncWithRetry(MethodInfo method, object?[]? args, RetryAttribute retryAttr)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return method.Invoke(_decorated, args);
            }
            catch (TargetInvocationException ex) when (IsTransientException(ex.InnerException) &&
                                                       attempt < retryAttr.MaxRetries)
            {
                attempt++;
                var delay = CalculateDelay(attempt, retryAttr);
                _logger.LogWarning("Retry {Attempt} for {Method}", attempt, method.Name);
                Thread.Sleep(delay);
            }
            catch (TargetInvocationException ex)
            {
                _logger.LogError(ex.InnerException, "Failed after {Attempt} attempts", attempt);
                throw ex.InnerException ?? ex;
            }
        }
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