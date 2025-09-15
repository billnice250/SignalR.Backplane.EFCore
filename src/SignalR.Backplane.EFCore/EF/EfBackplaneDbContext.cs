using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SignalR.Backplane.EFCore.Interfaces;
using SignalR.Backplane.EFCore.Models;
using System.Text.Json;

namespace SignalR.Backplane.EFCore.EF;

public class EfBackplaneDbContext(DbContextOptions<EfBackplaneDbContext> options) : DbContext(options), IBackplaneDbContext
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
            entity.HasKey(e => e.Id);
            // Auto-increment (Identity)
            entity.Property(e => e.Id)
                  .ValueGeneratedOnAdd();
            entity.Property(e => e.Payload)
                  .HasConversion(payloadConverter);

            // JSONB for Postgres
            if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                entity.Property(e => e.Payload).HasColumnType("jsonb");
            }

            entity.HasMany(e => e.Acks)
                  .WithOne(a => a.Message)
                  .HasForeignKey(a => a.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BackplaneSubscriber>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Acks)
                  .WithOne(a => a.Subscriber)
                  .HasForeignKey(a => a.SubscriberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BackplaneAck>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Auto-increment (Identity)
            entity.Property(e => e.Id)
                  .ValueGeneratedOnAdd();
            entity.HasIndex(a => new { a.MessageId, a.SubscriberId })
                  .IsUnique(); // ✅ one ack per subscriber per message
        });
    }

}
