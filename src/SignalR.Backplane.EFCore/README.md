# SignalR.Backplane.EFCore

An **EF Core–backed backplane for ASP.NET Core SignalR**.  
Enables **horizontal scale-out** across multiple application instances with:

- **Ack-based delivery**  
- **Subscriber heartbeat tracking**  
- **Configurable cleanup policies**

---

## 🚀 Installation

```bash
dotnet add package SignalR.Backplane.EFCore
```

---

## ⚡ Quick Start

### 1. Register services in `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR()
    .AddBackplaneDbContext<EfBackplaneDbContext>(
        options => options.UseSqlite(builder.Configuration.GetConnectionString("Main")),
        configure =>
        {
            configure.StoreSubscriberId = $"{Environment.MachineName}-{Guid.NewGuid()}";
            configure.AutoCreate = true;
        });

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHub<NotificationHub>("/hubs/notification");

app.MapGet("/randomMessage", async (IHubContext<NotificationHub> hubContext) =>
{
    var msg = $"Random-{Guid.NewGuid():N}";
    await hubContext.Clients.All.SendAsync("signalr", msg);
    return Results.Ok(new { Sent = msg });
});

app.Run();
```

👉 Run multiple instances of your app pointing at the **same database** to scale SignalR out across nodes.  
Each instance must use a unique `SubscriberId`.

---

## 🧭 Scale-Out Notes

- Use any EF Core provider as the shared store (Postgres, SQL Server, SQLite for dev/test).  
- Ensure each app instance sets a **unique** `StoreSubscriberId` (e.g., `hostname + GUID`).  
- If two stores use the same `StoreSubscriberId`, they are treated as **instances of the same subscriber** — acknowledgements from one apply to all (idempotent).  
- `AutoCreate = true` simplifies the first run, but in production run migrations explicitly.  
- Works seamlessly in **containers, Kubernetes, Azure App Service, or VMs** — no Redis required.

---

### ▶️ Running two instances locally (Kestrel)

Both servers share the same SQLite file for testing:

```bash
# Terminal 1
dotnet run --urls "https://localhost:5001"

# Terminal 2
dotnet run --urls "https://localhost:5002"
```

For production, use Postgres or SQL Server as the shared store.

---

### ▶️ Connect a .NET client

```csharp
using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:5002/hubs/notification") // match your server URL
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("signalr", msg =>
{
    Console.WriteLine($"[NotificationHub] Received: {msg}");
});

await connection.StartAsync();
Console.WriteLine($"State: {connection.State}"); // should be Connected

Console.WriteLine("Connected to SignalR NotificationHub.");
Console.ReadKey();

await connection.DisposeAsync();
```

✅ You’ll see the message arrive regardless of which instance you send it to.

---

## ✨ Features

- **Horizontal scalability** — run multiple SignalR instances against a shared DB  
- **EF Core backplane** — supports Postgres, SQL Server, and SQLite  
- **Ack-based message delivery** — ensures subscribers confirm receipt  
- **Subscriber heartbeat tracking**  
- **Configurable cleanup** — TTL, logical delete, or physical delete  
- **Multiple hub support** — via generic `BusHubLifetimeManager<THub>`  

---

## 📖 Links

- [NuGet Package](https://www.nuget.org/packages/SignalR.Backplane.EFCore)  
- [GitHub Repository](https://github.com/billnice250/SignalR.Backplane.EFCore)  

---

## 📜 License

MIT License © Bill Nice G. Havugukuri
