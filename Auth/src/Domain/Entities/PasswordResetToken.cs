namespace Domain.Entities;

public class PasswordResetToken
{
    public required string Id { get; init; }
    public required string UserId { get; set; }                
    public required string Token { get; set; }             
    public DateTime ExpiresAt { get; set; }        
    public DateTime CreatedAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; set; }
}