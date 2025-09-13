using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SignalR.Backplane.EFCore.Interfaces;
using SignalR.Backplane.EFCore.Models;
using System.Text.Json;

namespace SignalR.Backplane.EFCore.EF;

public class EfBacklplaneDbContext(DbContextOptions<EfBacklplaneDbContext> options) : DbContext(options), IBackplaneDbContext
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
