using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Backplane.EFCore.EF;
using SignalR.Backplane.EFCore.Sample;

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
