namespace Application.Models;

public sealed record ReserveInventoryResult(
    bool Success,
    string ReservationId,
    string ErrorMessage,
    ReserveInventoryFailureReason FailureReason,
    bool IsIdempotentReplay)
{
    public static ReserveInventoryResult Successful(
        string reservationId,
        bool isIdempotentReplay = false) =>
        new(true, reservationId, string.Empty, ReserveInventoryFailureReason.None, isIdempotentReplay);

    public static ReserveInventoryResult Failed(
        string errorMessage,
        ReserveInventoryFailureReason failureReason) =>
        new(false, string.Empty, errorMessage, failureReason, false);
}
