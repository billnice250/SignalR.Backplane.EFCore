using SignalR.Backplane.EFCore.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Backplane.EFCore.Interfaces;

public interface IBackplaneStore
{
    Task PublishAsync(string channel, BackplaneEnvelope payload, CancellationToken cancellationToken = default);
    IAsyncEnumerable<(BackplaneEnvelope Payload, long Id)> SubscribeAsync(string channel, CancellationToken cancellationToken = default);
    Task AckAsync(long messageId, string subscriberId, CancellationToken cancellationToken = default);
    Task AckForCurrentStoreAsync(long messageId, CancellationToken cancellationToken = default);
    Task RegisterSubscriberAsync(string subscriberId, CancellationToken cancellationToken = default);
    Task HeartbeatAsync(string subscriberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetActiveSubscribersAsync(CancellationToken cancellationToken = default);
    Task RunCleanerAsync(CancellationToken cancellationToken = default);
}
