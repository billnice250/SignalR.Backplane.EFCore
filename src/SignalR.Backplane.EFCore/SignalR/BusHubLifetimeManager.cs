using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using SignalR.Backplane.EFCore.Interfaces;
using SignalR.Backplane.EFCore.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Backplane.EFCore.SignalR;

public class BusHubLifetimeManager<THub> : HubLifetimeManager<THub> where THub : Hub
{
    private const string ChannelName = "signalr";
    private const string SubscriberId = "HubLifetimeManager";
    private readonly IBackplaneStore _bus;
    private readonly ConcurrentDictionary<string, HubConnectionContext> _connections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _groups = new();
    private readonly Task _setupTask;

    public BusHubLifetimeManager(IBackplaneStore bus)
    {
        _bus = bus;

        _setupTask = Task.Run(async () =>
        {
            await foreach (var (payload, id) in _bus.SubscribeAsync(ChannelName))
            {
                await Dispatch(payload);
                await _bus.AckAsync(id, SubscriberId);
            }
        });
    }

    public override Task OnConnectedAsync(HubConnectionContext connection)
    {
        _connections[connection.ConnectionId] = connection;
        return _bus.RegisterSubscriberAsync(connection.ConnectionId);
    }

    public override Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        foreach (var g in _groups.Values) g.Remove(connection.ConnectionId);
        return Task.CompletedTask;
    }

    public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        var group = _groups.GetOrAdd(groupName, _ => []);
        lock (group) group.Add(connectionId);
        return Task.CompletedTask;
    }

    public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        if (_groups.TryGetValue(groupName, out var group))
        {
            lock (group) group.Remove(connectionId);
        }
        return Task.CompletedTask;
    }

    private async Task Dispatch(BackplaneEnvelope env)
    {
        HubMessage msg;
        if (env.IsInvocationMessage)
        {
            if (string.IsNullOrWhiteSpace(env.Method))
                throw new ArgumentNullException(nameof(env.Method), "Method is required where IsInvocationMessage is true.");
            msg = new InvocationMessage(env.Method!, env.Args);
        }
        else
        {

            msg = new HubSimpleMessage
            {
                Category = env.Method,
                MessageText = env.MessageText
            };
        }
        switch (env.Type)
        {
            case NotificationType.all:
                foreach (var conn in _connections.Values)
                    if (env.Excluded == null || !env.Excluded.Contains(conn.ConnectionId))
                        await conn.WriteAsync(msg);
                break;

            case NotificationType.group:
                if (_groups.TryGetValue(env.Targets.FirstOrDefault()??string.Empty, out var group))
                {
                    foreach (var cid in group)
                        if ((env.Excluded == null || !env.Excluded.Contains(cid)) &&
                            _connections.TryGetValue(cid, out var conn))
                            await conn.WriteAsync(msg);
                }
                break;

            case NotificationType.conns:
                foreach (var cid in env.Targets)
                    if (_connections.TryGetValue(cid, out var conn))
                        await conn.WriteAsync(msg);
                break;

            case NotificationType.groups:
                foreach (var gname in env.Targets)
                    if (_groups.TryGetValue(gname, out var gset))
                        foreach (var cid in gset)
                            if (_connections.TryGetValue(cid, out var conn))
                                await conn.WriteAsync(msg);
                break;

            case NotificationType.user:
                foreach (var conn in _connections.Values.Where(c => env.Targets.Contains(c.UserIdentifier)))
                    await conn.WriteAsync(msg);
                break;
        }
    }

    private Task Publish(BackplaneEnvelope payload, CancellationToken cancellationToken)
    {
        return _bus.PublishAsync(ChannelName, payload, cancellationToken);
    }

    public override Task SendAllAsync(string methodName, object?[]? args, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.all, Method = methodName, Args = args ?? [] }, cancellationToken);

    public override Task SendUserAsync(string userId, string methodName, object?[]? args, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.user, Targets = [userId], Method = methodName, Args = args ?? [] }, cancellationToken);

    public override Task SendGroupAsync(string groupName, string methodName, object?[]? args, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.group, Targets = [groupName], Method = methodName, Args = args ?? [] }, cancellationToken);

    public override Task SendConnectionAsync(string connectionId, string methodName, object?[]? args, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.conn, Targets = [connectionId], Method = methodName, Args = args ?? [] }, cancellationToken);

    public override Task SendAllExceptAsync(string methodName, object?[]? args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.all, Method = methodName, Args = args ?? [], Excluded = excludedConnectionIds }, cancellationToken);

    public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object?[]? args, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.conns, Targets = connectionIds, Method = methodName, Args = args ?? [] }, cancellationToken);

    public override Task SendGroupExceptAsync(string groupName, string methodName, object?[]? args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.group, Targets = [groupName], Method = methodName, Args = args ?? [], Excluded = excludedConnectionIds }, cancellationToken);

    public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object?[]? args, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type =NotificationType.groups, Targets = groupNames, Method = methodName, Args = args ?? [] }, cancellationToken);

    public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object?[]? args, CancellationToken cancellationToken = default)
        => Publish(new BackplaneEnvelope { Type = NotificationType.user, Targets = userIds, Method = methodName, Args = args ?? [] }, cancellationToken);

}
