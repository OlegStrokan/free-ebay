using Infrastructure.Persistence.DbContext;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence;

internal static class TestDbContextFactory
{
    public static PaymentDbContext Create()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new PaymentDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
