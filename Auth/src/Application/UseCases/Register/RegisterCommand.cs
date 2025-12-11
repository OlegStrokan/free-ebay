namespace Application.UseCases.Register;

public record RegisterCommand(string Email, string Password, string Fullname, string Phone);

