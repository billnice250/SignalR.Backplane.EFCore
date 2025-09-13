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

    public EfBackplaneStore(IDbContextFactory<TContext> contextFactory, Options.EfBackplaneOptions options)
    {
        _contextFactory = contextFactory;
        _subscriberId = options.SubscriberId;
        _options = options;

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

        // Always validate connectivity afterwards
        using var test = _contextFactory.CreateDbContext();
        if (!test.Database.CanConnect())
        {
            throw new InvalidOperationException(
                "Backplane schema does not exist or cannot be reached. " +
                "If AutoCreate is disabled, please initialize the schema manually.");
        }

    }

    public async Task PublishAsync(string channel, BackplaneEnvelope payload, CancellationToken cancellationToken = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        db.Messages.Add(new BackplaneMessage { Channel = channel, Payload = payload });
        await db.SaveChangesAsync(cancellationToken);
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
        db.Add(new BackplaneAck { MessageId = messageId, SubscriberId = subscriberId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RegisterSubscriberAsync(string subscriberId, CancellationToken cancellationToken = default)
    {
        await using var db = _contextFactory.CreateDbContext();
        var existing = await db.Set<BackplaneSubscriber>().FindAsync([subscriberId], cancellationToken);
        if (existing == null)
            db.Add(new BackplaneSubscriber { Id = subscriberId });
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
}
