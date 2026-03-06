namespace Application.UseCases.UpdateUser;

public record UpdateUserCommand(
    string Id, string Email, string Fullname, string Phone);