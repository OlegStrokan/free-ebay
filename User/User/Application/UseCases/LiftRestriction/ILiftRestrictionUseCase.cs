namespace Application.UseCases.LiftRestriction;

public interface ILiftRestrictionUseCase
{
    Task<LiftRestrictionResponse> ExecuteAsync(LiftRestrictionCommand command);
}
