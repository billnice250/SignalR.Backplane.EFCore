using Microsoft.EntityFrameworkCore;
using SignalR.Backplane.EFCore.EF;

namespace SignalR.Backplane.EFCore.Tests;

public class TestDbContextFactory : IDbContextFactory<EfBackplaneDbContext>
{
    private readonly DbContextOptions<EfBackplaneDbContext> _options;
    public TestDbContextFactory(DbContextOptions<EfBackplaneDbContext> options) => _options = options;
    public EfBackplaneDbContext CreateDbContext() => new EfBackplaneDbContext(_options);
}
