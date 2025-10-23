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
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, string status)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(appointmentId);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }

                appointment.AppointmentStatus = status;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Appointment status updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating appointment status" });
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
}
