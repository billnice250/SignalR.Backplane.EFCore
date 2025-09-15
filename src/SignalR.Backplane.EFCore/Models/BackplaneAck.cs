using System;
namespace SignalR.Backplane.EFCore.Models;
public record BackplaneAck
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public BackplaneMessage Message { get; set; } = default!;
    public string SubscriberId { get; set; } = default!;
    public BackplaneSubscriber Subscriber { get; set; } = default!;
    public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;
}
