using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:5002/hubs/notification") // match your app’s URL
    .WithAutomaticReconnect()
    .Build();

// Subscribe to messages
connection.On<string>("signalr", msg =>
{
    Console.WriteLine($"[NotificationHub] Received: {msg}");
});

await connection.StartAsync();
Console.WriteLine(connection.State); // should be Connected

Console.WriteLine("Connected to SignalR NotificationHub.");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();

await connection.DisposeAsync();
