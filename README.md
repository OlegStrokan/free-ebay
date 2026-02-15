# Gabriela

2 apps, one extremely complicated ebay with crypto support + public api, and second one is less complicated but also stupidly complex xiaoping-express

ebay stack:

Tech info (not complete)
user service

user service probably should be a rest api, but 90% of other microservices use grpc and don't expose any methods for public, all use gateway, so to make it consistent
i prefer consistency to logic. user-service will be grpc microservice, yes it's overkill, but zhizn' igra, igray krasivo


auth service

some auth stuff. also grpc

order service:
i am idiot so i admire complexity: saga based transaction with sprinkle of outbox transactions, kafka workers, ddd aggregates, event sourcing and cqrs. 

8 fucking layers to create order

Internal IDs: Use Guid (OrderId, CustomerId, ProductId)
External IDs: Use string (PaymentId, TrackingId, ShipmentId, RefundId)

// ❌ BAD: Step modifies saga state directly
public async Task<StepResult> ExecuteAsync(...)
{
await RegisterWebhook();

    // Step shouldn't do this!
    sagaState.Status = SagaStatus.WaitingForEvent;  
    
    return StepResult.Success();
}

// ✅ GOOD: Step communicates via result
public async Task<StepResult> ExecuteAsync(...)
{
await RegisterWebhook();

    // Step signals its intent
    return StepResult.SuccessResult(new Dictionary {
        ["SagaState"] = "WaitingForEvent"
    });
}
```

### Metadata is a Communication Protocol
```
┌─────────────────┐
│  Step (Child)   │
│                 │
│  "Hey parent,   │
│  I need to wait │
│  for webhook"   │
└────────┬────────┘
│ Metadata
│ ["SagaState"] = "WaitingForEvent"
▼
┌─────────────────┐
│ SagaBase (Parent│
│                 │
│ "OK, I'll pause │
│ the saga for you│
└─────────────────┘

for direct approach step whould need access to saga state, and this violates all shit
using metadata we signal about shit and saga base class will handle thi shit - event driven saga




Isolation in domain entity: 

Orders over $1000 require manual approval

Year 2024: You put this check inside the Apply method. You save an order for $1200.

Year 2025: The business changes the rule: "Orders over $500 require manual approval." You update the code.

The Disaster: You try to load that $1200 order from 2024. The Apply method runs, sees $1200, checks the new $500 limit, finds it wasn't "approved" under the new rules, and throws an error. Your historical data is now unreadable.

By isolating the state change, you ensure that once an event is written to the store, the Apply method will always be able to rebuild that state, regardless of how business rules change in the future.



quesiton:
where idempotency key comes from? form frontend or from gateway layer? 
what it should be? is this good way to handle idempontecy?



we had cool retry decorator 
```
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
```

the problem what it's a fucking bomb so please use this:

```
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // Automatically handles 5xx and 408 errors
        .Or<SocketException>()      // Handles network connection issues
        .Or<IOException>()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100 + Random.Shared.Next(0, 100))
        );
}
// Gateway 1
builder.Services.AddHttpClient<IPaymentGateway, PaymentGateway>(client => {
    client.BaseAddress = new Uri("https://api.payments.com");
}).AddPolicyHandler(GetRetryPolicy());

// Gateway 2
builder.Services.AddHttpClient<IInventoryGateway, InventoryGateway>(client => {
    client.BaseAddress = new Uri("https://api.inventory.com");
}).AddPolicyHandler(GetRetryPolicy());

```

T0: Customer in EU clicks "Place Order"
→ Request goes to EU-WEST-1
→ Network latency to US (300ms)

T1 (100ms): EU processes request
OrderCreatedEvent { OrderId: <guid-A>, CustomerId: 123, Items: [ProductA] }
→ Starts replicating to US...

T2 (200ms): Customer gets impatient, clicks again
→ Browser sends ANOTHER request
→ Round-robin load balancer routes to US-EAST-1 (because EU is "slow")

T3 (250ms): US processes SECOND request (doesn't know about first)
OrderCreatedEvent { OrderId: <guid-B>, CustomerId: 123, Items: [ProductA] }
→ Starts replicating to EU...

T4 (500ms): Both regions see TWO orders from same customer
❓ Which one is real? Did customer intend 1 order or 2?


it's still one method for everything, but we don't use dispatchProxy via reflection 
which is more voodoo than Reagan economic type shit

saga handler it's just listen for kafka and run executeASync for specific saga (sagaBase method which is already part of every saga because it's abstract class)