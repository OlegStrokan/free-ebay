namespace Application.UseCases.CreateUser;

public record CreateUserCommand(string Email, string Password, string Fullname, string Phone);