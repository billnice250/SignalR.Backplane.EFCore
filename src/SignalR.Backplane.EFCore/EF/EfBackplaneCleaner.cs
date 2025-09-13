using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SignalR.Backplane.EFCore.Models;
using SignalR.Backplane.EFCore.Options;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Backplane.EFCore.EF
{
    public class EfBackplaneCleaner<TContext>(IDbContextFactory<TContext> factory, EfBackplaneOptions options) : BackgroundService where TContext : DbContext
    {
        private readonly IDbContextFactory<TContext> _factory = factory;
        private readonly EfBackplaneOptions _options = options;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await using var db = _factory.CreateDbContext();
                var activeSubs = await db.Set<BackplaneSubscriber>()
                    .Where(s => s.LastSeen >= System.DateTime.UtcNow - _options.HeartbeatTimeout)
                    .Select(s => s.Id)
                    .ToListAsync(stoppingToken);

                var msgs = db.Set<BackplaneMessage>().Include(m => m.Acks).Where(m => !m.IsDeleted);

                if (_options.CleanupStrategy == CleanupStrategy.AckBased || _options.CleanupStrategy == CleanupStrategy.AckOrTtlBased)
                {
                    var acked = msgs.Where(m => activeSubs.All(s => m.Acks.Any(a => a.SubscriberId == s)));
                    foreach (var msg in acked)
                        ApplyDeletion(msg);
                }

                if ((_options.CleanupStrategy == CleanupStrategy.TtlBased || _options.CleanupStrategy == CleanupStrategy.AckOrTtlBased))
                {
                    var expired = msgs.Where(m => System.DateTime.UtcNow - m.CreatedAt > _options.RetentionTime);
                    foreach (var msg in expired)
                        ApplyDeletion(msg);
                }

                await db.SaveChangesAsync(stoppingToken);
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
        }

        private void ApplyDeletion(BackplaneMessage msg)
        {
            if (_options.CleanupMode == CleanupMode.Logical)
                msg.IsDeleted = true;
            else if (_options.CleanupMode == CleanupMode.Physical)
                msg.IsDeleted = true; // EF will remove if Remove() is called in future
        }
    }
}
