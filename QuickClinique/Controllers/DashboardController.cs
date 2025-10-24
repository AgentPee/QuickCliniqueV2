using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Attributes;

namespace QuickClinique.Controllers
{
    [ClinicStaffOnly]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return RedirectToAction("Login", "Clinicstaff");
            }

            var dashboardData = new DashboardViewModel
            {
                // Get today's appointments
                TodaysAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == DateOnly.FromDateTime(DateTime.Today))
                    .OrderBy(a => a.QueueNumber)
                    .ToListAsync(),

                // Get pending appointments
                PendingAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.AppointmentStatus == "Pending")
                    .OrderBy(a => a.DateBooked)
                    .Take(10)
                    .ToListAsync(),

                // Get recent notifications
                RecentNotifications = await _context.Notifications
                    .Include(n => n.Patient)
                    .Where(n => n.ClinicStaffId == clinicStaffId)
                    .OrderByDescending(n => n.NotifDateTime)
                    .Take(5)
                    .ToListAsync(),

                // Get statistics
                TotalAppointments = await _context.Appointments.CountAsync(),
                PendingCount = await _context.Appointments.CountAsync(a => a.AppointmentStatus == "Pending"),
                ConfirmedCount = await _context.Appointments.CountAsync(a => a.AppointmentStatus == "Confirmed"),
                CompletedCount = await _context.Appointments.CountAsync(a => a.AppointmentStatus == "Completed"),
                TotalPatients = await _context.Students.CountAsync(),
                AvailableSlots = await _context.Schedules.CountAsync(s => s.IsAvailable == "Yes" && s.Date >= DateOnly.FromDateTime(DateTime.Today))
            };

            return View(dashboardData);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppointmentStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                Console.WriteLine($"UpdateAppointmentStatus called with appointmentId: {request?.appointmentId}, status: {request?.status}");
                
                if (request == null)
                {
                    Console.WriteLine("Request is null");
                    return Json(new { success = false, message = "Invalid request" });
                }
                
                var appointment = await _context.Appointments.FindAsync(request.appointmentId);
                if (appointment == null)
                {
                    Console.WriteLine($"Appointment not found with ID: {request.appointmentId}");
                    return Json(new { success = false, message = "Appointment not found" });
                }

                Console.WriteLine($"Found appointment: {appointment.AppointmentId}, Current status: {appointment.AppointmentStatus}");

                // Update appointment status
                appointment.AppointmentStatus = request.status;
                
                // Update queue status based on appointment status
                switch (request.status)
                {
                    case "Confirmed":
                        appointment.QueueStatus = "Waiting";
                        break;
                    case "In Progress":
                        appointment.QueueStatus = "Being Served";
                        break;
                    case "Completed":
                        appointment.QueueStatus = "Completed";
                        break;
                    case "Cancelled":
                        appointment.QueueStatus = "Cancelled";
                        break;
                    default:
                        appointment.QueueStatus = "Waiting";
                        break;
                }

                await _context.SaveChangesAsync();
                Console.WriteLine($"Appointment updated successfully. New status: {appointment.AppointmentStatus}, QueueStatus: {appointment.QueueStatus}");

                return Json(new { success = true, message = "Appointment status updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating appointment status: {ex.Message}");
                return Json(new { success = false, message = "Error updating appointment status: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification(int patientId, string content)
        {
            try
            {
                var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
                if (clinicStaffId == null)
                {
                    return Json(new { success = false, message = "Staff not authenticated" });
                }

                var notification = new Notification
                {
                    ClinicStaffId = clinicStaffId.Value,
                    PatientId = patientId,
                    Content = content,
                    NotifDateTime = DateTime.Now,
                    IsRead = "No"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending notification" });
            }
        }

        public async Task<IActionResult> GetQueueStatus()
        {
            var todaysAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .Where(a => a.Schedule.Date == DateOnly.FromDateTime(DateTime.Today))
                .OrderBy(a => a.QueueNumber)
                .ToListAsync();

            return Json(new { 
                success = true, 
                data = todaysAppointments.Select(a => new {
                    appointmentId = a.AppointmentId,
                    patientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                    queueNumber = a.QueueNumber,
                    status = a.AppointmentStatus,
                    timeSlot = $"{a.Schedule.StartTime} - {a.Schedule.EndTime}",
                    reason = a.ReasonForVisit
                })
            });
        }
    }

    public class DashboardViewModel
    {
        public List<Appointment> TodaysAppointments { get; set; } = new();
        public List<Appointment> PendingAppointments { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
        public int TotalAppointments { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CompletedCount { get; set; }
        public int TotalPatients { get; set; }
        public int AvailableSlots { get; set; }
    }

    public class UpdateStatusRequest
    {
        public int appointmentId { get; set; }
        public string status { get; set; } = string.Empty;
    }
}
