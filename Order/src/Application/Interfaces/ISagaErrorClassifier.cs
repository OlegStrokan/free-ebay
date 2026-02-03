namespace Application.Interfaces;

public interface ISagaErrorClassifier
{
    bool IsTransient(Exception ex);
}