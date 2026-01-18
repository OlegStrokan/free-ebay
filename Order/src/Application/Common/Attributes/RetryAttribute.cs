namespace Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RetryAttribute(
    int maxRetries = 3,
    int delayMilliseconds = 1000,
    bool exponentialBackoff = true)
    : Attribute
{
    public int MaxRetries { get; } = maxRetries;
    public int DelayMilliseconds { get; } = delayMilliseconds;
    public bool ExponentialBackoff { get; } = exponentialBackoff;
}