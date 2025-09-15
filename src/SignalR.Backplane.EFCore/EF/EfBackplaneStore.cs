using Microsoft.EntityFrameworkCore;
using SignalR.Backplane.EFCore.Interfaces;
using SignalR.Backplane.EFCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Backplane.EFCore.EF;
public class EfBackplaneStore<TContext> : IBackplaneStore
    where TContext : DbContext, IBackplaneDbContext
{
    private readonly IDbContextFactory<TContext> _contextFactory;
    private readonly string _subscriberId;
    private readonly Options.EfBackplaneOptions _options;
    private readonly EfBackplaneCleaner<TContext> _cleaner;

    public EfBackplaneStore(IDbContextFactory<TContext> contextFactory, Options.EfBackplaneOptions options, EfBackplaneCleaner<TContext> cleaner)
    {
        _contextFactory = contextFactory;
        _subscriberId = options.StoreSubscriberId;
        _options = options;
        _cleaner = cleaner;

        if (_options.AutoCreate)
        {
            using var db = _contextFactory.CreateDbContext();
            try
            {
                // EnsureCreated returns true if it created, false if it already existed
                db.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to create the backplane schema. Ensure the database is accessible or run migrations manually.",
                    ex);
            }
        }

        // ✅ Safe, provider-agnostic insert-or-update
        EnsureSubscriber();
    }

    public async Task PublishAsync(string channel, BackplaneEnvelope payload, CancellationToken cancellationToken = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        db.Messages.Add(new BackplaneMessage { Channel = channel, Payload = payload });
        await db.SaveChangesAsync(cancellationToken);
    }

    public void EnsureSubscriber()
    {
        // Always validate connectivity afterwards
        using var db = _contextFactory.CreateDbContext();
        if (!db.Database.CanConnect())
        {
            throw new InvalidOperationException(
                "Backplane schema does not exist or cannot be reached. " +
                "If AutoCreate is disabled, please initialize the schema manually.");
        }
        try
        {
            // Try insert first
            db.Set<BackplaneSubscriber>().Add(new BackplaneSubscriber
            {
                Id = _subscriberId,
                LastSeen = DateTime.UtcNow
            });

            db.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Already exists → update instead
            var existing = db.Set<BackplaneSubscriber>().Find(_subscriberId);
            if (existing != null)
            {
                existing.LastSeen = DateTime.UtcNow;
                db.Set<BackplaneSubscriber>().Update(existing);
                db.SaveChanges();
            }

        }
    }


    public async IAsyncEnumerable<(BackplaneEnvelope Payload, long Id)> SubscribeAsync(string channel, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await using var db = _contextFactory.CreateDbContext();
            var msgs = await db.Set<BackplaneMessage>()
                .Include(m => m.Acks)
                .Where(m => m.Channel == channel && !m.IsDeleted && !m.Acks.Any(a => a.SubscriberId == _subscriberId))
                .OrderBy(m => m.Id)
                .ToListAsync(cancellationToken);

            foreach (var msg in msgs)
            {
                yield return (msg.Payload, msg.Id);
            }

            await Task.Delay(_options.PollInterval, cancellationToken);
        }
    }

    public async Task AckAsync(long messageId, string subscriberId, CancellationToken cancellationToken = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        db.Acks.Add(new BackplaneAck { MessageId = messageId, SubscriberId = subscriberId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RegisterSubscriberAsync(string subscriberId, CancellationToken cancellationToken = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var existing = await db.Set<BackplaneSubscriber>().FindAsync([subscriberId], cancellationToken);
        if (existing == null)
            db.Subscribers.Add(new BackplaneSubscriber { Id = subscriberId });
        else
            existing.LastSeen = System.DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task HeartbeatAsync(string subscriberId, CancellationToken cancellationToken = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var sub = await db.Set<BackplaneSubscriber>().FindAsync([subscriberId], cancellationToken);
        if (sub != null)
        {
            sub.LastSeen = System.DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<string>> GetActiveSubscribersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var timeout = System.DateTime.UtcNow - _options.HeartbeatTimeout;
        return await db.Set<BackplaneSubscriber>()
            .Where(s => s.LastSeen >= timeout)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public Task RunCleanerAsync(CancellationToken cancellationToken = default) => _cleaner.RunManually(cancellationToken);

    public Task AckForCurrentStoreAsync(long messageId, CancellationToken cancellationToken = default)
    {
        return AckAsync(messageId, _subscriberId, cancellationToken);
    }
}
