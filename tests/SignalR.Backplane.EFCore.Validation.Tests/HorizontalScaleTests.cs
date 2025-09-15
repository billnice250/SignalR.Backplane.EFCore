//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using SignalR.Backplane.EFCore.EF;
//using Microsoft.AspNetCore.SignalR.Client;
//using Microsoft.AspNetCore.Http.Connections;

//namespace SignalR.Backplane.EFCore.Tests;

//public class HorizontalScaleTests
//{
//    [Test]
//    public async Task MessageFromServerA_ReachesClientOnServerB()
//    {
//        var dbConn = "DataSource=shared_backplane.db";

//        using var serverA = StartServer(5051, dbConn, "A");
//        using var serverB = StartServer(5052, dbConn, "B");

//        var clientA = new HubConnectionBuilder()
//            .WithUrl(new Uri("http://localhost:5051/testhub"), HttpTransportType.LongPolling)
//            .WithAutomaticReconnect()
//            .Build();

//        var clientB = new HubConnectionBuilder()
//            .WithUrl(new Uri("http://localhost:5052/testhub"), HttpTransportType.LongPolling)
//            .WithAutomaticReconnect()
//            .Build();

//        var received = new TaskCompletionSource<string>();

//        clientB.On<string>("ReceiveMessage", msg =>
//        {
//            received.TrySetResult(msg);
//        });

//        await clientA.StartAsync();
//        await clientB.StartAsync();

//        await clientA.InvokeAsync("SendMessage", "hello from A");

//        var result = await Task.WhenAny(received.Task, Task.Delay(5000));
//        Assert.True(result == received.Task, "Message not received across servers");
//        Assert.That(received.Task.Result, Is.EqualTo("hello from A"));

//        await clientA.DisposeAsync();
//        await clientB.DisposeAsync();
//    }

//    private static WebApplication StartServer(int port, string dbConn, string name)
//    {
//        var builder = WebApplication.CreateBuilder();
//        builder.WebHost.UseUrls($"http://localhost:{port}");
//        builder.Services
//        .AddSignalR()
//            .AddBackplaneDbContext<EfBackplaneDbContext>(opts => 
//            {
//                opts.UseSqlite(dbConn);
//            }, (backplaneOpts) =>
//            {
//                backplaneOpts.StoreSubscriberId = $"server-{name}";
//                backplaneOpts.AutoCreate = true;
//            });

//        builder.Services.AddSingleton<EfBackplaneCleaner<EfBackplaneDbContext>>();

//        var app = builder.Build();
//        app.MapHub<TestHub>("/testhub");

//        // run in background
//        _ = app.RunAsync();
//        return app;
//    }

//    public class TestHub : Hub
//    {
//        public async Task SendMessage(string message)
//        {
//            await Clients.All.SendAsync("ReceiveMessage", message);
//        }
//    }

//}
