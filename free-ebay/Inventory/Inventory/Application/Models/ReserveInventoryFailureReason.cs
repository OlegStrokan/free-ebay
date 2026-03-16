namespace Application.Models;

public enum ReserveInventoryFailureReason
{
    None = 0,
    Validation = 1,
    ProductNotFound = 2,
    InsufficientStock = 3,
    Unknown = 4
}
