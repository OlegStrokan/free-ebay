namespace Domain.Exceptions;

/// <summary>
/// Thrown when an optimistic-concurrency check fails after all retries are exhausted.
/// Callers can catch this specific type instead of relying on InvalidOperationException
/// string-matching.
/// </summary>
public class ConcurrencyConflictException(string aggregateType, string aggregateId, int attempts)
    : DomainException(
        $"Concurrency conflict for {aggregateType} {aggregateId} persisted after {attempts} retries.");
