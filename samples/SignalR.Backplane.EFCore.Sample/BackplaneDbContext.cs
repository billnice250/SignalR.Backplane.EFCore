using Microsoft.EntityFrameworkCore;
using SignalR.Backplane.EFCore.Models;

namespace SignalR.Backplane.EFCore.Sample
{
    public class BackplaneDbContext : DbContext
    {
        public BackplaneDbContext(DbContextOptions<BackplaneDbContext> options)
            : base(options)
        {
        }

        public DbSet<BackplaneMessage> Messages => Set<BackplaneMessage>();
        public DbSet<BackplaneAck> Acks => Set<BackplaneAck>();
        public DbSet<BackplaneSubscriber> Subscribers => Set<BackplaneSubscriber>();
    }
}
