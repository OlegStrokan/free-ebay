using Domain.Entities.DeliveryInfo;
using Domain.Entities.User;

namespace Application.UseCases.CreateUser;

public record CreateUserResponse(string Id, string Email, string Fullname, UserStatus Status);