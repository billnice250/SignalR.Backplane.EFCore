using System;

namespace SignalR.Backplane.EFCore.Options;

public enum CleanupStrategy
{
    None,
    AckBased,
    TtlBased,
    AckOrTtlBased
}

public enum CleanupMode
{
    Logical,
    Physical
}

public class EfBackplaneOptions
{
    /// <summary>
    /// Unique identifier for this subscriber.
    /// If two stores use the same <see cref="StoreSubscriberId"/>, they are treated as
    /// multiple instances of the same subscriber, and acknowledgements from one
    /// instance are considered equivalent for all (idempotent).
    /// </summary>
    public string StoreSubscriberId { get; set; } = Environment.MachineName;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    ///  When set to true, it will Call  db.Database.EnsureCreated(); on the underlying IBackplaneDbContext implementation.
    ///  <br></br>Make  sure this acceptable for your use case. 
    ///  <br></br>Otherwise you should setup the database your self before hand.
    /// </summary>
    public bool AutoCreate { get; set; } = true;

    // Heartbeat
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(45);

    // Cleanup
    public CleanupStrategy CleanupStrategy { get; set; } = CleanupStrategy.AckOrTtlBased;
    public CleanupMode CleanupMode { get; set; } = CleanupMode.Logical;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RetentionTime { get; set; } = TimeSpan.FromDays(7);
}
