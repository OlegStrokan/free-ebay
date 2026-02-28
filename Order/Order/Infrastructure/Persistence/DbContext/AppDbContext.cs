using Application.Models;
using Application.Sagas.Persistence;
using Domain.Entities;
using Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using IdempotencyRecord = Application.Interfaces.IdempotencyRecord;


namespace Infrastructure.Persistence.DbContext;

public class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<SagaState> SagaStates => Set<SagaState>();
    public DbSet<SagaStepLog> SagaStepLogs => Set<SagaStepLog>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<AggregateSnapshot> AggregateSnapshots => Set<AggregateSnapshot>();
    public DbSet<ReturnRequestLookup> ReturnRequestLookups => Set<ReturnRequestLookup>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new OutboxMessageConfiguration());
        builder.ApplyConfiguration(new SagaStateConfiguration());
        builder.ApplyConfiguration(new SagaStepLogConfiguration());
        builder.ApplyConfiguration(new IdempotencyRecordConfiguration());
        builder.ApplyConfiguration(new DomainEventConfiguration());
        builder.ApplyConfiguration(new ProcessedEventConfiguration());
        builder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        builder.ApplyConfiguration(new AggregateSnapshotConfiguration());
        builder.ApplyConfiguration(new ReturnRequestLookupConfiguration());
    }
    
}