using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SignalR.Backplane.EFCore.Models;
using SignalR.Backplane.EFCore.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Backplane.EFCore.EF
{
    public class EfBackplaneCleaner<TContext>(IDbContextFactory<TContext> factory, EfBackplaneOptions options) : BackgroundService 
        where TContext : DbContext
    {
        private readonly IDbContextFactory<TContext> _factory = factory;
        private readonly EfBackplaneOptions _options = options;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCleaningTaskNow(stoppingToken);
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
        }

        private async Task RunCleaningTaskNow(CancellationToken stoppingToken)
        {
            using var db = _factory.CreateDbContext();
            System.DateTime dateTime = System.DateTime.UtcNow - _options.HeartbeatTimeout;

            var activeSubs = await db.Set<BackplaneSubscriber>()
                .Where(s => s.LastSeen >= dateTime)
                .Select(s => s.Id)
                .ToListAsync(stoppingToken);

            var msgTable = db.Set<BackplaneMessage>();

            // Always start with non-deleted messages
            var msgs = msgTable
                .Include(m => m.Acks)
                .Where(m => !m.IsDeleted);

            // --- Ack-based cleanup ---
            if (_options.CleanupStrategy is CleanupStrategy.AckBased or CleanupStrategy.AckOrTtlBased)
            {
                var activeCount = activeSubs.Count;

                // Load only candidates with enough acks
                var candidates = await msgs
                    .Where(m => m.Acks
                        .Select(a => a.SubscriberId)
                        .Distinct()
                        .Count() >= activeCount)
                    .ToListAsync(stoppingToken);

                // Verify in memory (safety net)
                var fullyAcked = candidates
                    .Where(m => activeSubs.All(s => m.Acks.Any(a => a.SubscriberId == s)));

                foreach (var msg in fullyAcked)
                    ApplyDeletion(msg, msgTable);
            }

            // --- TTL-based cleanup ---
            if (_options.CleanupStrategy is CleanupStrategy.TtlBased or CleanupStrategy.AckOrTtlBased)
            {
                var cutoff = DateTime.UtcNow - _options.RetentionTime;

                var expired = await msgs
                    .Where(m => m.CreatedAt < cutoff)
                    .ToListAsync(stoppingToken);

                foreach (var msg in expired)
                    ApplyDeletion(msg, msgTable);
            }

            await db.SaveChangesAsync(stoppingToken);
        }

        private void ApplyDeletion(BackplaneMessage msg, DbSet<BackplaneMessage> msgTable)
        {
            if (_options.CleanupMode == CleanupMode.Logical)
            {
                msg.IsDeleted = true;
                msgTable.Update(msg);
            }
            else if (_options.CleanupMode == CleanupMode.Physical)
            {
                msgTable.Remove(msg);
            }
        }

        public async Task<bool> RunManually(CancellationToken stoppingToken)
        {
            await RunCleaningTaskNow(stoppingToken);
            return true;
        }
    }
}
