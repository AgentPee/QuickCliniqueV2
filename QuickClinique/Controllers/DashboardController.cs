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

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var dashboardData = new
            {
                // Statistics
                stats = new
                {
                    totalAppointments = await _context.Appointments.CountAsync(),
                    pendingCount = await _context.Appointments.CountAsync(a => a.AppointmentStatus == "Pending"),
                    confirmedCount = await _context.Appointments.CountAsync(a => a.AppointmentStatus == "Confirmed"),
                    completedCount = await _context.Appointments.CountAsync(a => a.AppointmentStatus == "Completed"),
                    totalPatients = await _context.Students.CountAsync(),
                    availableSlots = await _context.Schedules.CountAsync(s => s.IsAvailable == "Yes" && s.Date >= DateOnly.FromDateTime(DateTime.Today))
                },
                // Today's appointments
                todaysAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == DateOnly.FromDateTime(DateTime.Today))
                    .OrderBy(a => a.QueueNumber)
                    .Select(a => new
                    {
                        appointmentId = a.AppointmentId,
                        patientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                        queueNumber = a.QueueNumber,
                        status = a.AppointmentStatus,
                        timeSlot = $"{a.Schedule.StartTime} - {a.Schedule.EndTime}",
                        reason = a.ReasonForVisit
                    })
                    .ToListAsync(),
                // Pending appointments
                pendingAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.AppointmentStatus == "Pending")
                    .OrderBy(a => a.DateBooked)
                    .Take(10)
                    .Select(a => new
                    {
                        appointmentId = a.AppointmentId,
                        patientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                        scheduleDate = a.Schedule.Date.ToString("MMM dd, yyyy"),
                        timeSlot = $"{a.Schedule.StartTime} - {a.Schedule.EndTime}",
                        reason = a.ReasonForVisit
                    })
                    .ToListAsync(),
                // Recent notifications
                recentNotifications = await _context.Notifications
                    .Include(n => n.Patient)
                    .Where(n => n.ClinicStaffId == clinicStaffId)
                    .OrderByDescending(n => n.NotifDateTime)
                    .Take(5)
                    .Select(n => new
                    {
                        content = n.Content,
                        dateTime = n.NotifDateTime.ToString("MMM dd, HH:mm")
                    })
                    .ToListAsync()
            };

            return Json(new { success = true, data = dashboardData });
        }

        // GET: Dashboard/GetMessages - Get ALL messages between students and clinic staff (shared inbox)
        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not logged in" });
            }

            // Get current clinic staff's userId
            var clinicStaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClinicStaffId == clinicStaffId.Value);

            if (clinicStaff == null)
            {
                return Json(new { success = false, error = "Clinic staff not found" });
            }

            // Get all clinic staff user IDs (for filtering)
            var allClinicStaffUserIds = await _context.Clinicstaffs
                .Select(c => c.UserId)
                .ToListAsync();

            // Get ALL messages involving any clinic staff (shared inbox for all staff)
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => allClinicStaffUserIds.Contains(m.SenderId) || allClinicStaffUserIds.Contains(m.ReceiverId))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new {
                    messageId = m.MessageId,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    senderName = m.Sender.Name,
                    receiverName = m.Receiver.Name,
                    message = m.Message1,
                    createdAt = m.CreatedAt,
                    isSent = m.SenderId == clinicStaff.UserId
                })
                .ToListAsync();

            return Json(new { success = true, data = messages, currentUserId = clinicStaff.UserId });
        }

        // POST: Dashboard/SendMessage - Send a reply message to student
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendStaffMessageRequest request)
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not logged in" });
            }

            // Get clinic staff's userId
            var clinicStaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClinicStaffId == clinicStaffId.Value);

            if (clinicStaff == null)
            {
                return Json(new { success = false, error = "Clinic staff not found" });
            }

            // Get the receiver (student) UserId
            if (request.ReceiverId <= 0)
            {
                return Json(new { success = false, error = "Receiver not specified" });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, error = "Message cannot be empty" });
            }

            var message = new Message
            {
                SenderId = clinicStaff.UserId,
                ReceiverId = request.ReceiverId,
                Message1 = request.Message,
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Message sent successfully",
                data = new {
                    messageId = message.MessageId,
                    senderId = message.SenderId,
                    receiverId = message.ReceiverId,
                    senderName = clinicStaff.User.Name,
                    message = message.Message1,
                    createdAt = message.CreatedAt,
                    isSent = true
                }
            });
        }

        // GET: Dashboard/GetPatientMessages - Get ALL messages grouped by student (shared inbox)
        [HttpGet]
        public async Task<IActionResult> GetPatientMessages()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not logged in" });
            }

            // Get clinic staff's userId
            var clinicStaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClinicStaffId == clinicStaffId.Value);

            if (clinicStaff == null)
            {
                return Json(new { success = false, error = "Clinic staff not found" });
            }

            // Get all clinic staff user IDs (for filtering)
            var allClinicStaffUserIds = await _context.Clinicstaffs
                .Select(c => c.UserId)
                .ToListAsync();

            // Get ALL messages involving any clinic staff (shared inbox)
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => allClinicStaffUserIds.Contains(m.SenderId) || allClinicStaffUserIds.Contains(m.ReceiverId))
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Group messages by patient (student) - identify patients by excluding clinic staff
            var patientMessages = messages
                .GroupBy(m => allClinicStaffUserIds.Contains(m.SenderId) ? m.ReceiverId : m.SenderId)
                .Select(g => new {
                    patientUserId = g.Key,
                    patientName = g.First(m => m.SenderId == g.Key || m.ReceiverId == g.Key).SenderId == g.Key ? 
                                 g.First(m => m.SenderId == g.Key).Sender.Name : 
                                 g.First(m => m.ReceiverId == g.Key).Receiver.Name,
                    lastMessage = g.First().Message1,
                    lastMessageTime = g.First().CreatedAt,
                    unreadCount = 0 // Can be enhanced later with read/unread tracking
                })
                .ToList();

            return Json(new { success = true, data = patientMessages });
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

    public class SendStaffMessageRequest
    {
        public int ReceiverId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
