namespace Application.Models;

public sealed record ReleaseInventoryResult(
    bool Success,
    string ErrorMessage,
    bool IsIdempotentReplay)
{
    public static ReleaseInventoryResult Successful(
        bool isIdempotentReplay = false,
        string message = "") =>
        new(true, message, isIdempotentReplay);

    public static ReleaseInventoryResult Failed(string errorMessage) =>
        new(false, errorMessage, false);
}
