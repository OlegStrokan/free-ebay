namespace Application.UseCases.LiftRestriction;

public record LiftRestrictionCommand(string TargetUserId, string ActorUserId);
