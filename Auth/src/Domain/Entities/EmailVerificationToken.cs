namespace Domain.Entities;

public class EmailVerificationToken
{

    public required string Id { get; init; }
    public required string UserId { get; set; }                // Reference to User Service
    public required string Token { get; set; }              // Unique token (GUID)
    public DateTime ExpiresAt { get; set; }        // Valid for 24 hours
    public DateTime CreatedAt { get; init; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    
}