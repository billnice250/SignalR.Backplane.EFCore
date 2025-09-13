using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SignalR.Backplane.EFCore.EF;
using SignalR.Backplane.EFCore.Interfaces;
using SignalR.Backplane.EFCore.Models;
using SignalR.Backplane.EFCore.Options;
using Xunit;

namespace SignalR.Backplane.EFCore.Tests;

public class BasicTests
{

    [Fact]
    public async Task CanPublishAndAckMessage()
    {
        string sessionId = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"DataSource=test_{sessionId}.db")
            .Options;

        await using var db = new TestDbContext(options);
        db.Database.OpenConnection();

        try
        {
            var status = db.Database.EnsureCreated();
            if (!status)
            {
                string Value = "Database not created";

                Console.WriteLine(Value);
                throw new Exception(Value);
            }
            await db.Database.MigrateAsync();
        }
        catch 
        {

        }


        var store = new EfBackplaneStore<TestDbContext>(
            new DbContextFactory(options),
            new EfBackplaneOptions { SubscriberId = Guid.NewGuid().ToString() });

        BackplaneEnvelope msg = new BackplaneEnvelope
        {
            Args = [],
            Method = "TestMessage",
            IsInvocationMessage = false,
            MessageText = "Hello From EF backplane"
        };
        Console.WriteLine($"Sending: {msg}");
        await store.PublishAsync("test", msg);
        var enumerator = store.SubscribeAsync("test").GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        var (payload, id) = enumerator.Current;
        Console.WriteLine($"Received: {payload}");
        Assert.NotNull(payload);
        await store.AckAsync(id, "test");
        Assert.Equal(msg.MessageText, payload.MessageText);
    }
    private class DbContextFactory : IDbContextFactory<TestDbContext>
    {
        private readonly DbContextOptions<TestDbContext> _options;
        public DbContextFactory(DbContextOptions<TestDbContext> options) => _options = options;
        public TestDbContext CreateDbContext() => new TestDbContext(_options);
    }
}



public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options), IBackplaneDbContext
{

    public DbSet<BackplaneMessage> Messages => Set<BackplaneMessage>();
    public DbSet<BackplaneAck> Acks => Set<BackplaneAck>();
    public DbSet<BackplaneSubscriber> Subscribers => Set<BackplaneSubscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var payloadConverter = new ValueConverter<BackplaneEnvelope, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<BackplaneEnvelope>(v, (JsonSerializerOptions?)null)!);

        modelBuilder.Entity<BackplaneMessage>(entity =>
        {
            entity.Property(e => e.Payload)
                  .HasConversion(payloadConverter);

            // Optionally make it JSONB when using PostgreSQL
            if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                entity.Property(e => e.Payload).HasColumnType("jsonb");
            }
        });
    }
}
public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=test.db")
            .Options;

        return new TestDbContext(options);
    }
}
