using Microsoft.AspNetCore.SignalR.Protocol;
using System.Collections.Generic;

namespace SignalR.Backplane.EFCore.Models;

public enum NotificationType
{
    all, user, group, conn, conns, groups
}
public record BackplaneEnvelope
{
    public NotificationType Type { get; set; } = default!; 
    /// <summary>
    /// Type Identifiers. [id1,id2,..] or [groupName]
    /// </summary>
    public IReadOnlyList<string> Targets { get; set; } = [];

    /// <summary>
    ///  if true, Method will be invoke with the associated arguments.
    ///  <br></br>Otherwise MessageText will be passed as HubSimpleMessage
    /// </summary>
    public bool IsInvocationMessage { get; set; } = true;
    /// <summary>
    /// CallBackName function name or message category applicable.
    /// </summary>
    public string? Method { get; set; } = null!;
    /// <summary>
    ///  Args of the CallBack that will be passed to CallBackName.
    /// </summary>
    public object?[] Args { get; set; } = [];

    /// <summary>
    /// Simple text message or potentially a serializable text content 
    /// </summary>
    public string? MessageText { get; set; } = null;

    /// <summary>
    /// Exclude type Identifiers. [id1,id2,..] or [groupName]
    /// </summary>
    public IReadOnlyList<string>? Excluded { get; set; }
}


public class HubSimpleMessage:HubMessage
{
    public string? Category { get; set; }
    public string? MessageText { get; set; }
}