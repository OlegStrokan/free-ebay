

using System;

namespace Domain.Entities.User;



public enum UserStatus {
    Active = 0,
    Blocked = 1,

}
public class UserEntity
{
    public required string Id { get; init; }
    public required string Fullname { get; set; }
    public required string Password { get; set; }
    public required  string Email { get; set; } 
    
    public required  string Phone { get; set; }

    public required DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
     public UserStatus Status { get; set; } =  UserStatus.Active;
    // should be updated later to dynamically retrieve default role
    // public string RoleId { get; set; } = "2";
    // public List<DeliveryInfo.DeliveryInfo> DeliveryInfo { get; set; } = [];
}