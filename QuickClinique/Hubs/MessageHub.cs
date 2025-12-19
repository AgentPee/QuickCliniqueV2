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

    // Method for students to join emergency notification group
    public async Task JoinEmergencyGroup(int studentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"emergency_student_{studentId}");
    }

    // Method for students to leave emergency notification group
    public async Task LeaveEmergencyGroup(int studentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"emergency_student_{studentId}");
    }
}

