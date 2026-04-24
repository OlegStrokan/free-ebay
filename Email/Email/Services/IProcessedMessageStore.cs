namespace Email.Services;

public interface IProcessedMessageStore
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<bool> IsProcessedAsync(Guid messageId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken);
}