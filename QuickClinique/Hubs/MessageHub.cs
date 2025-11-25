using Microsoft.AspNetCore.SignalR;

namespace QuickClinique.Hubs;

public class MessageHub : Hub
{
    // Method to join a user-specific group
    public async Task JoinUserGroup(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    // Method to leave a user-specific group
    public async Task LeaveUserGroup(int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    // Method for clinic staff to join the clinic staff group (shared inbox)
    public async Task JoinClinicStaffGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "clinic_staff");
    }

    // Method for clinic staff to leave the clinic staff group
    public async Task LeaveClinicStaffGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "clinic_staff");
    }
}

