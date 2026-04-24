namespace Domain.Exceptions;

public sealed class UniqueConstraintViolationException : DomainException
{
    public UniqueConstraintViolationException(string? constraintName, Exception innerException)
        : base(
            string.IsNullOrWhiteSpace(constraintName)
                ? "Unique constraint violation."
                : $"Unique constraint violation: '{constraintName}'.",
            innerException)
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}