# SignalR.Backplane.EFCore

EF Core–backed backplane for ASP.NET Core SignalR that enables horizontal scale-out across multiple app instances, with ack-based delivery, subscriber tracking, and cleanup policies.

## 🚀 Installation

```bash
dotnet add package SignalR.Backplane.EFCore
```

## ⚡ Quick Start

### 1. Create a DbContext (or use the default)

```csharp
public class CustomBackplaneDbContext : DbContext, IBackplaneDbContext
{
    public DbSet<BackplaneMessage> Messages => Set<BackplaneMessage>();
    public DbSet<BackplaneAck> Acks => Set<BackplaneAck>();
    public DbSet<BackplaneSubscriber> Subscribers => Set<BackplaneSubscriber>();

    public CustomBackplaneDbContext(DbContextOptions<CustomBackplaneDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store Payload as JSON/JSONB depending on provider
        var converter = new ValueConverter<BackplaneEnvelope, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<BackplaneEnvelope>(v, (JsonSerializerOptions?)null)!);

        modelBuilder.Entity<BackplaneMessage>(entity =>
        {
            entity.Property(e => e.Payload).HasConversion(converter);
            if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
                entity.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}
```

### 2. Register services in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BackplaneDbContext>(options =>
    options.UseSqlite("Data Source=backplane.db"));

builder.Services.AddSignalR()
    .AddBackplaneDbContext<BackplaneDbContext>(
        options => options.UseSqlite("Data Source=backplane.db"),
        configure =>
        {
            configure.SubscriberId = $"{Environment.MachineName}-{Guid.NewGuid()}";
            configure.AutoCreate = true;
        });

var app = builder.Build();

app.MapHub<NotificationHub>("/hubs/notification");

app.Run();
```

Run multiple instances of your app pointing at the same database to scale out SignalR across nodes. Each instance should use a unique `SubscriberId`.

## 🧭 Scale-Out Notes

- Use any EF Core provider as the shared store (Postgres, SQL Server, SQLite for dev/test).
- Ensure each app instance sets a unique `SubscriberId` (e.g., hostname + GUID).
- `AutoCreate = true` simplifies first run; in production, prefer running migrations once during deployment.
- Works in containers, Kubernetes, Azure App Service, or VMs—no Redis required.

### Docker Compose example (two instances)

```yaml
version: "3.9"
services:
  signalr-a:
    build: ./samples/SignalR.Backplane.EFCore.Sample
    ports:
      - "5000:5000"
    volumes:
      - ./samples/SignalR.Backplane.EFCore.Sample:/app
    environment:
      - DOTNET_USE_POLLING_FILE_WATCHER=1
    command: ["dotnet", "run", "--urls", "http://0.0.0.0:5000"]

  signalr-b:
    build: ./samples/SignalR.Backplane.EFCore.Sample
    ports:
      - "5001:5001"
    volumes:
      - ./samples/SignalR.Backplane.EFCore.Sample:/app
    environment:
      - DOTNET_USE_POLLING_FILE_WATCHER=1
    command: ["dotnet", "run", "--urls", "http://0.0.0.0:5001"]
```

Both instances share the same SQLite file via the bind-mounted sample folder, suitable for local testing. For production, use Postgres or SQL Server as the shared store.

## ✨ Features

- ✅ **Horizontal scalability**: scale SignalR across multiple app instances via a shared database  
- ✅ **EF Core backplane** (Postgres, SQL Server, SQLite supported)  
- ✅ **Ack-based message delivery** (ensures subscribers mark messages delivered)  
- ✅ **Subscriber heartbeat tracking**  
- ✅ **Configurable cleanup** (TTL, logical or physical deletion)  
- ✅ **Multiple hubs supported** (open generic `BusHubLifetimeManager<THub>`)  

---

## 📖 Links

- [NuGet Package](https://www.nuget.org/packages/SignalR.Backplane.EFCore)
- [GitHub Repository](https://github.com/billnice250/SignalR.Backplane.EFCore)

---

## 📜 License

MIT License © Bill Nice G. Havugukuri
