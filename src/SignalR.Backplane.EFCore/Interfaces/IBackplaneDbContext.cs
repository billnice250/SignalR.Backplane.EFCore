using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SignalR.Backplane.EFCore.Models;
using System.Threading.Tasks;
using System.Threading;

namespace SignalR.Backplane.EFCore.Interfaces;

public interface IBackplaneDbContext
{
    DbSet<BackplaneMessage> Messages { get; }
    DbSet<BackplaneAck> Acks { get; }
    DbSet<BackplaneSubscriber> Subscribers { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
