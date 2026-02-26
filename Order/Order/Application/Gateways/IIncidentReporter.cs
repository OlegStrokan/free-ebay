using Application.Common.Enums;

namespace Application.Gateways;

public record IncidentAlert(
    string AlertType,
    Guid OrderId,
    string? RefundId,
    string Message,
    AlertSeverity Severity);

public record InterventionTicket(
    Guid OrderId,
    string? RefundId,
    string Issue,
    string SuggestedAction);


public interface IIncidentReporter
{
    Task SendAlertAsync(IncidentAlert alert, CancellationToken cancellationToken);
    Task CreateInterventionTicketAsync(InterventionTicket ticket, CancellationToken cancellationToken);
}
