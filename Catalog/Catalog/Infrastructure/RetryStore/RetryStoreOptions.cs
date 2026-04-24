namespace Infrastructure.RetryStore;

public sealed class RetryStoreOptions
{
    public const string SectionName = "RetryStore";
    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=catalog_retry;Username=postgres;Password=postgres";
    public int WorkerRetryLimit { get; set; } = 15;
    public int WorkerPollIntervalSeconds { get; set; } = 180;
    public int WorkerBatchSize { get; set; } = 50;
}
