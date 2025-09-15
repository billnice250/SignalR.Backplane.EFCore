using System;
using System.Collections.Generic;

namespace SignalR.Backplane.EFCore.Models;
public record BackplaneSubscriber
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public ICollection<BackplaneAck> Acks { get; set; } = new List<BackplaneAck>();
}
