using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SpeakHalo.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public const string AdminGroup = "admins";

    public async Task SendNotificationToUser(string userId, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveNotification", message);
    }

    public async Task SendCallUpdate(string userId, object callData)
    {
        await Clients.User(userId).SendAsync("ReceiveCallUpdate", callData);
    }

    public async Task SendAppointmentUpdate(string userId, object appointmentData)
    {
        await Clients.User(userId).SendAsync("ReceiveAppointmentUpdate", appointmentData);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.Identity?.Name;
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        // Add admin users to the admins group for real-time admin events
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.Identity?.Name;
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin")
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminGroup);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
