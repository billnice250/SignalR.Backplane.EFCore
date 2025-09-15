using System;
using System.Collections.Generic;
namespace SignalR.Backplane.EFCore.Models;

public record BackplaneMessage
{
    public long Id { get; set; }
    public string Channel { get; set; } = default!;
    public BackplaneEnvelope Payload { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    public ICollection<BackplaneAck> Acks { get; set; } = [];
}
