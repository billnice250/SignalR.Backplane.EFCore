using Microsoft.AspNetCore.SignalR;

namespace SignalR.Backplane.EFCore.Sample
{
    public class NotificationHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
