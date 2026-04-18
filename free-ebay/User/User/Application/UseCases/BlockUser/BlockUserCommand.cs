namespace Application.UseCases.BlockUser;

public record BlockUserCommand(string TargetUserId, string ActorUserId, string Reason);
