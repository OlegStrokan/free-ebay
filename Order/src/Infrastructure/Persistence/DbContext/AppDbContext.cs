using Application.Models;
using Application.Sagas.Persistence;
using Domain.Entities;
using Infrastructure.Persistence.Configurations;
using Infrastructure.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using IdempotencyRecord = Application.Interfaces.IdempotencyRecord;

namespace Infrastructure.Persistence.DbContext;

public class AppDbContext(DbContextOptions<AppDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<SagaState> SagaStates => Set<SagaState>();
    public DbSet<SagaStepLog> SagaStepLogs => Set<SagaStepLog>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<DomainEvent> DomainEvents => Set<DomainEvent>();
    public DbSet<OrderReadModel> OrderReadModels => Set<OrderReadModel>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfiguration(new OutboxMessageConfiguration());
        builder.ApplyConfiguration(new SagaStateConfiguration());
        builder.ApplyConfiguration(new SagaStepLogConfiguration());
        builder.ApplyConfiguration(new IdempotencyRecordConfiguration());
        builder.ApplyConfiguration(new DomainEventConfiguration());
        builder.ApplyConfiguration(new OrderReadModelConfiguration());
    }
    
}