using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalR.Backplane.EFCore;
using SignalR.Backplane.EFCore.Sample;

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
