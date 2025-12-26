using Domain.Entities.User;

namespace Application.UseCases.UpdateUser;

public record UpdateUserResponse(
    string Id, string Email, string Fullname, string Phone, UserStatus Status);