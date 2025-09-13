using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignalR.Backplane.EFCore.Models;
public record BackplaneAck
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public long MessageId { get; set; }
    public string SubscriberId { get; set; } = default!;
    public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;
}
