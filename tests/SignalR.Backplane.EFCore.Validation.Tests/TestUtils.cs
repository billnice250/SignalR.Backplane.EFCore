using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using SignalR.Backplane.EFCore.EF;
using SignalR.Backplane.EFCore.Options;
using System;

namespace SignalR.Backplane.EFCore.Tests;

public static class TestUtils
{
    public static (EfBackplaneStore<EfBackplaneDbContext> store, EfBackplaneDbContext db) CreateStore(string subscriberId = "test", double? ttlSeconds = null, string? dbConnName=null)
    {
        var efOptions = new EfBackplaneOptions
        {
            StoreSubscriberId = subscriberId,
            AutoCreate = true,
            HeartbeatTimeout = TimeSpan.FromMinutes(5)
        };

        if (ttlSeconds is not null)
        {
            efOptions.RetentionTime = TimeSpan.FromSeconds(ttlSeconds.Value);
        }
        return CreateStore(efOptions, dbConnName);
    }

    public static (EfBackplaneStore<EfBackplaneDbContext> store, EfBackplaneDbContext db) CreateStore(EfBackplaneOptions efBackplaneOptions, string? dbConnName = null)
    {
        string sessionnDb = $"unitTest_{Guid.NewGuid().ToString()}.db";
        var options = new DbContextOptionsBuilder<EfBackplaneDbContext>()
            .UseInMemoryDatabase(dbConnName ?? sessionnDb)
            .Options;

        var db = new EfBackplaneDbContext(options);
        db.Database.EnsureCreated();

        TestDbContextFactory testDbContextFactory = new(options);
        var cleaner = new EfBackplaneCleaner<EfBackplaneDbContext>(testDbContextFactory, efBackplaneOptions);
        var store = new EfBackplaneStore<EfBackplaneDbContext>(testDbContextFactory, efBackplaneOptions, cleaner);
        return (store, db);
    }
}
