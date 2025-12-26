using Domain.Entities.User;

namespace Application.UseCases.GetUserById;

public record GetUserByIdResponse(string Id, string Email, string Fullname, string Phone, UserStatus Status);