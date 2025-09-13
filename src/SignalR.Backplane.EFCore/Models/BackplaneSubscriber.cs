using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SignalR.Backplane.EFCore.Models;
public record BackplaneSubscriber
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
