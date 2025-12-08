using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Attributes;
using QuickClinique.Hubs;
using QuickClinique.Services;

namespace QuickClinique.Controllers
{
    [ClinicStaffOnly]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly INotificationService _notificationService;

        public DashboardController(ApplicationDbContext context, IHubContext<MessageHub> hubContext, INotificationService notificationService)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
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
                // Get today's appointments - sorted by queue number descending, then by time descending
                TodaysAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == DateOnly.FromDateTime(DateTime.Today))
                    .OrderByDescending(a => a.QueueNumber)
                    .ThenByDescending(a => a.Schedule.StartTime)
                    .ToListAsync(),

                // Get pending appointments
                PendingAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.AppointmentStatus == "Pending")
                    .OrderBy(a => a.DateBooked)
                    .Take(10)
                    .ToListAsync(),

                // Get recent notifications - unread first, then read
                RecentNotifications = await _context.Notifications
                    .Include(n => n.Patient)
                    .Where(n => n.ClinicStaffId == clinicStaffId)
                    .OrderByDescending(n => n.IsRead == "No")
                    .ThenByDescending(n => n.NotifDateTime)
                    .Take(10)
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

                // Store original status to check for changes
                var originalStatus = appointment.AppointmentStatus;

                // Update appointment status
                appointment.AppointmentStatus = request.status;
                
                // Load schedule to potentially update availability
                await _context.Entry(appointment).Reference(a => a.Schedule).LoadAsync();
                
                // Handle schedule availability based on status changes
                if (appointment.Schedule != null)
                {
                    // Check if status changed to Cancelled
                    bool statusChangedToCancelled = originalStatus != "Cancelled" && request.status == "Cancelled";
                    
                    // Check if status changed from Cancelled to active
                    bool statusChangedFromCancelled = originalStatus == "Cancelled" && 
                                                     request.status != "Cancelled" &&
                                                     (request.status == "Pending" || request.status == "Confirmed" || request.status == "In Progress");
                    
                    if (statusChangedToCancelled)
                    {
                        // Check how many other active appointments exist for this schedule
                        var activeAppointmentCount = await _context.Appointments
                            .CountAsync(a => a.ScheduleId == appointment.ScheduleId &&
                                a.AppointmentId != appointment.AppointmentId &&
                                (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "In Progress"));
                        
                        // Make schedule available if there are less than 2 active appointments
                        if (activeAppointmentCount < 2)
                        {
                            appointment.Schedule.IsAvailable = "Yes";
                        }
                    }
                    else if (statusChangedFromCancelled)
                    {
                        // Check total active appointments for this schedule
                        var activeAppointmentCount = await _context.Appointments
                            .CountAsync(a => a.ScheduleId == appointment.ScheduleId &&
                                (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "In Progress"));
                        
                        // Mark schedule as unavailable only when it reaches 2 patients
                        if (activeAppointmentCount >= 2)
                        {
                            appointment.Schedule.IsAvailable = "No";
                        }
                    }
                }
                
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
                    NotifDateTime = TimeZoneHelper.GetPhilippineTime(),
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
                // Today's appointments - sorted by queue number descending, then by time descending
                todaysAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == DateOnly.FromDateTime(DateTime.Today))
                    .OrderByDescending(a => a.QueueNumber)
                    .ThenByDescending(a => a.Schedule.StartTime)
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
                // Recent notifications - unread first, then read with redirect URLs
                recentNotifications = (await _context.Notifications
                    .Include(n => n.Patient)
                    .Where(n => n.ClinicStaffId == clinicStaffId)
                    .OrderByDescending(n => n.IsRead == "No")
                    .ThenByDescending(n => n.NotifDateTime)
                    .Take(10)
                    .ToListAsync())
                    .Select(n => new
                    {
                        notificationId = n.NotificationId,
                        content = n.Content,
                        dateTime = n.NotifDateTime.ToString("MMM dd, HH:mm"),
                        isRead = n.IsRead == "Yes",
                        patientName = n.Patient != null ? $"{n.Patient.FirstName} {n.Patient.LastName}" : null,
                        patientId = n.PatientId,
                        redirectUrl = GetNotificationRedirectUrl(n)
                    })
                    .ToList(),
                // Unread notification count
                unreadNotificationCount = await _context.Notifications
                    .Where(n => n.ClinicStaffId == clinicStaffId && n.IsRead == "No")
                    .CountAsync()
            };

            return Json(new { success = true, data = dashboardData });
        }

        // POST: Dashboard/MarkNotificationAsRead - Mark a notification as read
        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead([FromBody] MarkNotificationReadRequest request)
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == request.NotificationId && n.ClinicStaffId == clinicStaffId);

            if (notification == null)
            {
                return Json(new { success = false, error = "Notification not found" });
            }

            notification.IsRead = "Yes";
            await _context.SaveChangesAsync();

            // Get updated unread count
            var unreadCount = await _context.Notifications
                .Where(n => n.ClinicStaffId == clinicStaffId && n.IsRead == "No")
                .CountAsync();

            return Json(new { success = true, message = "Notification marked as read", unreadCount });
        }

        // POST: Dashboard/MarkAllNotificationsAsRead - Mark all notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.ClinicStaffId == clinicStaffId && n.IsRead == "No")
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = "Yes";
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "All notifications marked as read", unreadCount = 0 });
        }

        // GET: Dashboard/GetUnreadNotificationCount - Get count of unread notifications
        [HttpGet]
        public async Task<IActionResult> GetUnreadNotificationCount()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var unreadCount = await _context.Notifications
                .Where(n => n.ClinicStaffId == clinicStaffId && n.IsRead == "No")
                .CountAsync();

            return Json(new { success = true, unreadCount });
        }

        // POST: Dashboard/ClearReadNotifications - Delete all read notifications
        [HttpPost]
        public async Task<IActionResult> ClearReadNotifications()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var readNotifications = await _context.Notifications
                .Where(n => n.ClinicStaffId == clinicStaffId && n.IsRead == "Yes")
                .ToListAsync();

            if (readNotifications.Any())
            {
                _context.Notifications.RemoveRange(readNotifications);
                await _context.SaveChangesAsync();
            }

            // Get updated unread count
            var unreadCount = await _context.Notifications
                .Where(n => n.ClinicStaffId == clinicStaffId && n.IsRead == "No")
                .CountAsync();

            return Json(new { success = true, message = "Read notifications cleared successfully", unreadCount });
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

            // Get all clinic staff with their user IDs for name lookup
            var clinicStaffLookup = await _context.Clinicstaffs
                .Select(c => new { c.UserId, c.FirstName, c.LastName })
                .ToListAsync();

            // Get ALL messages involving any clinic staff (shared inbox for all staff)
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => allClinicStaffUserIds.Contains(m.SenderId) || allClinicStaffUserIds.Contains(m.ReceiverId))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Project messages with clinic staff full name if sender is clinic staff
            var messageData = messages.Select(m => {
                var senderStaff = clinicStaffLookup.FirstOrDefault(c => c.UserId == m.SenderId);
                var senderFullName = senderStaff != null 
                    ? $"{senderStaff.FirstName} {senderStaff.LastName}" 
                    : null;

                return new {
                    messageId = m.MessageId,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    senderName = m.Sender.Name,
                    senderFullName = senderFullName,
                    receiverName = m.Receiver.Name,
                    message = m.Message1,
                    createdAt = m.CreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    isSent = m.SenderId == clinicStaff.UserId
                };
            }).ToList();

            return Json(new { success = true, data = messageData, currentUserId = clinicStaff.UserId });
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
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Reload message with navigation properties for SignalR broadcast
            var savedMessage = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageId == message.MessageId);

            // Get clinic staff full name
            var senderFullName = $"{clinicStaff.FirstName} {clinicStaff.LastName}";

            var messageData = new
            {
                messageId = savedMessage.MessageId,
                senderId = savedMessage.SenderId,
                receiverId = savedMessage.ReceiverId,
                senderName = savedMessage.Sender.Name,
                senderFullName = senderFullName,
                receiverName = savedMessage.Receiver.Name,
                message = savedMessage.Message1,
                createdAt = savedMessage.CreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                isSent = true
            };

            // Broadcast to student (receiver)
            await _hubContext.Clients.Group($"user_{request.ReceiverId}").SendAsync("ReceiveMessage", new
            {
                messageId = savedMessage.MessageId,
                senderId = savedMessage.SenderId,
                receiverId = savedMessage.ReceiverId,
                senderName = savedMessage.Sender.Name,
                receiverName = savedMessage.Receiver.Name,
                message = savedMessage.Message1,
                createdAt = savedMessage.CreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                isSent = false // For student, this is a received message
            });

            // Broadcast to all clinic staff (shared inbox)
            await _hubContext.Clients.Group("clinic_staff").SendAsync("ReceiveMessage", messageData);

            return Json(new { 
                success = true, 
                message = "Message sent successfully",
                data = messageData
            });
        }

        // GET: Dashboard/GetPatientDirectory - list of patients for ID search
        [HttpGet]
        public async Task<IActionResult> GetPatientDirectory()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not logged in" });
            }

            var patients = await _context.Students
                .Include(s => s.User)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new
                {
                    userId = s.UserId,
                    idNumber = s.Idnumber,
                    fullName = $"{s.FirstName} {s.LastName}"
                })
                .ToListAsync();

            return Json(new { success = true, data = patients });
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

        // GET: Dashboard/GetActiveEmergencies - Get all unresolved emergencies
        [HttpGet]
        public async Task<IActionResult> GetActiveEmergencies()
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var emergencies = await _context.Emergencies
                .Where(e => !e.IsResolved)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    emergencyId = e.EmergencyId,
                    studentId = e.StudentId,
                    studentName = e.StudentName,
                    studentIdNumber = e.StudentIdNumber,
                    location = e.Location,
                    needs = e.Needs,
                    createdAt = e.CreatedAt
                })
                .ToListAsync();

            return Json(new { success = true, data = emergencies });
        }

        // POST: Dashboard/MarkEmergencyResolved - Mark an emergency as resolved
        [HttpPost]
        public async Task<IActionResult> MarkEmergencyResolved([FromBody] MarkEmergencyResolvedRequest request)
        {
            var clinicStaffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (clinicStaffId == null)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            var emergency = await _context.Emergencies
                .FirstOrDefaultAsync(e => e.EmergencyId == request.EmergencyId);

            if (emergency == null)
            {
                return Json(new { success = false, error = "Emergency not found" });
            }

            emergency.IsResolved = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Emergency marked as resolved" });
        }

        // GET: Dashboard/GetStudentByIdNumber - Get student by ID number
        [HttpGet]
        [ClinicStaffOnly]
        public async Task<IActionResult> GetStudentByIdNumber(string idNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idNumber))
                {
                    return Json(new { success = false, error = "ID Number is required" });
                }

                if (!int.TryParse(idNumber, out int studentIdNumber))
                {
                    return Json(new { success = false, error = "Invalid ID Number format" });
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Idnumber == studentIdNumber);

                if (student == null)
                {
                    return Json(new { success = false, error = "Student not found" });
                }

                return Json(new { 
                    success = true, 
                    data = new {
                        fullName = student.FullName,
                        firstName = student.FirstName,
                        lastName = student.LastName,
                        email = student.Email,
                        phoneNumber = student.PhoneNumber
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting student by ID number: {ex.Message}");
                return Json(new { 
                    success = false, 
                    error = "An error occurred while fetching student information." 
                });
            }
        }

        // POST: Dashboard/CreateWalkInAppointment - Create walk-in appointment
        [HttpPost]
        [ClinicStaffOnly]
        public async Task<IActionResult> CreateWalkInAppointment([FromBody] CreateWalkInAppointmentRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return Json(new { success = false, error = "Invalid request data" });
                }

                if (string.IsNullOrWhiteSpace(request.StudentIdNumber))
                {
                    return Json(new { success = false, error = "Student ID Number is required" });
                }

                if (!int.TryParse(request.StudentIdNumber, out int studentIdNumber))
                {
                    return Json(new { success = false, error = "Invalid Student ID Number format" });
                }

                // Validate student ID exists
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Idnumber == studentIdNumber);

                if (student == null)
                {
                    return Json(new { 
                        success = false, 
                        error = "Student ID not found. Appointment cannot be created." 
                    });
                }

                // Validate other required fields
                if (string.IsNullOrWhiteSpace(request.StudentFullName))
                {
                    return Json(new { success = false, error = "Student Full Name is required" });
                }

                // Validate that the submitted name matches the student's name from database
                if (!string.Equals(request.StudentFullName.Trim(), student.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { 
                        success = false, 
                        error = "Student name does not match the ID number. Please verify the ID number." 
                    });
                }

                if (string.IsNullOrWhiteSpace(request.AppointmentDate))
                {
                    return Json(new { success = false, error = "Appointment Date is required" });
                }

                if (string.IsNullOrWhiteSpace(request.AppointmentTime))
                {
                    return Json(new { success = false, error = "Appointment Time is required" });
                }

                if (string.IsNullOrWhiteSpace(request.ReasonForVisit))
                {
                    return Json(new { success = false, error = "Reason for Visit is required" });
                }

                if (string.IsNullOrWhiteSpace(request.PriorityLevel))
                {
                    return Json(new { success = false, error = "Priority Level is required" });
                }

                // Parse date and time
                if (!DateOnly.TryParse(request.AppointmentDate, out DateOnly appointmentDate))
                {
                    return Json(new { success = false, error = "Invalid appointment date format" });
                }

                if (!TimeOnly.TryParse(request.AppointmentTime, out TimeOnly appointmentTime))
                {
                    return Json(new { success = false, error = "Invalid appointment time format" });
                }

                // Calculate end time (default to 30 minutes after start time)
                // Convert to DateTime, add minutes, then convert back to TimeOnly
                var dateTime = appointmentDate.ToDateTime(appointmentTime);
                var endDateTime = dateTime.AddMinutes(30);
                var endTime = TimeOnly.FromDateTime(endDateTime);

                // Find or create schedule for this date and time
                var schedule = await _context.Schedules
                    .FirstOrDefaultAsync(s => s.Date == appointmentDate && 
                                           s.StartTime == appointmentTime &&
                                           s.EndTime == endTime);

                if (schedule == null)
                {
                    // Create a new schedule for walk-in
                    schedule = new Schedule
                    {
                        Date = appointmentDate,
                        StartTime = appointmentTime,
                        EndTime = endTime,
                        IsAvailable = "Yes" // Start as available, will be marked unavailable if needed
                    };
                    _context.Schedules.Add(schedule);
                    await _context.SaveChangesAsync();
                }

                // Check how many active appointments exist for this schedule BEFORE creating the appointment
                var existingAppointmentCount = await _context.Appointments
                    .CountAsync(a => a.ScheduleId == schedule.ScheduleId &&
                        (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "In Progress"));
                
                // For non-priority appointments, enforce the 2-patient limit
                // Priority appointments can bypass this limit
                if (request.PriorityLevel != "Priority" && existingAppointmentCount >= 2)
                {
                    return Json(new { 
                        success = false, 
                        error = "This time slot is already fully booked (2 patients). Priority appointments can bypass this limit." 
                    });
                }

                // Get today's date for queue calculation
                var today = DateOnly.FromDateTime(DateTime.Today);
                int queueNumber;

                // Handle queue placement based on priority
                if (request.PriorityLevel == "Priority")
                {
                    // Priority: Place immediately next in queue
                    // Find the current "In Progress" appointment or the next waiting appointment
                    var currentAppointment = await _context.Appointments
                        .Include(a => a.Schedule)
                        .Where(a => a.Schedule.Date == today && 
                                   a.AppointmentStatus == "In Progress")
                        .FirstOrDefaultAsync();

                    if (currentAppointment != null)
                    {
                        // Place right after the current appointment
                        queueNumber = currentAppointment.QueueNumber + 1;
                        
                        // Shift all subsequent appointments by 1
                        var subsequentAppointments = await _context.Appointments
                            .Include(a => a.Schedule)
                            .Where(a => a.Schedule.Date == today && 
                                       a.QueueNumber >= queueNumber)
                            .ToListAsync();

                        foreach (var appt in subsequentAppointments)
                        {
                            appt.QueueNumber += 1;
                        }
                    }
                    else
                    {
                        // No current appointment, place at the front
                        var firstWaiting = await _context.Appointments
                            .Include(a => a.Schedule)
                            .Where(a => a.Schedule.Date == today && 
                                       (a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "Pending") &&
                                       a.QueueStatus == "Waiting")
                            .OrderBy(a => a.QueueNumber)
                            .FirstOrDefaultAsync();

                        if (firstWaiting != null)
                        {
                            queueNumber = firstWaiting.QueueNumber;
                            
                            // Shift all appointments from this queue number onwards
                            var subsequentAppointments = await _context.Appointments
                                .Include(a => a.Schedule)
                                .Where(a => a.Schedule.Date == today && 
                                           a.QueueNumber >= queueNumber)
                                .ToListAsync();

                            foreach (var appt in subsequentAppointments)
                            {
                                appt.QueueNumber += 1;
                            }
                        }
                        else
                        {
                            // No appointments today, start at 1
                            queueNumber = 1;
                        }
                    }
                }
                else
                {
                    // Regular: Place at the end of the queue
                    var lastQueue = await _context.Appointments
                        .Include(a => a.Schedule)
                        .Where(a => a.Schedule.Date == today)
                        .OrderByDescending(a => a.QueueNumber)
                        .FirstOrDefaultAsync();
                    
                    queueNumber = (lastQueue?.QueueNumber ?? 0) + 1;
                }

                // Create the appointment
                var now = DateTime.Now;
                var timeSelected = appointmentDate.ToDateTime(appointmentTime);
                
                var appointment = new Appointment
                {
                    PatientId = student.StudentId,
                    ScheduleId = schedule.ScheduleId,
                    ReasonForVisit = request.ReasonForVisit,
                    Symptoms = "Walk-in appointment",
                    DateBooked = DateOnly.FromDateTime(now),
                    AppointmentStatus = "Confirmed", // Walk-ins are automatically confirmed
                    QueueStatus = "Waiting",
                    QueueNumber = queueNumber,
                    TimeSelected = timeSelected, // Date and time selected for the appointment
                    CreatedAt = now // When appointment was submitted
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Check how many active appointments exist for this schedule after creating the appointment
                var activeAppointmentCount = await _context.Appointments
                    .CountAsync(a => a.ScheduleId == schedule.ScheduleId &&
                        (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "In Progress"));
                
                // Mark schedule as unavailable when it reaches 2 patients (including priority appointments)
                // Note: Priority appointments can still be added beyond this limit, but the slot is marked unavailable
                if (activeAppointmentCount >= 2)
                {
                    schedule.IsAvailable = "No";
                    await _context.SaveChangesAsync();
                }

                // Send notifications to all clinic staff about the new walk-in appointment (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.NotifyNewAppointmentAsync(appointment);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[NOTIFICATION ERROR] Failed to send notification for walk-in appointment {appointment.AppointmentId}: {ex.Message}");
                    }
                });

                return Json(new { 
                    success = true, 
                    message = "Appointment successfully added.",
                    appointmentId = appointment.AppointmentId,
                    queueNumber = appointment.QueueNumber
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating walk-in appointment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { 
                    success = false, 
                    error = "An error occurred while creating the appointment. Please try again." 
                });
            }
        }

        /// <summary>
        /// Determines the redirect URL based on notification content and type
        /// </summary>
        private string GetNotificationRedirectUrl(Notification notification)
        {
            var content = notification.Content ?? "";
            var patientId = notification.PatientId;

            // Check notification content to determine type and redirect URL
            if (content.Contains("appointment booked", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("appointment set", StringComparison.OrdinalIgnoreCase))
            {
                // For appointment notifications, redirect to appointments management
                // If patient ID is available and valid, we could filter by patient
                if (patientId > 0)
                {
                    // Option 1: Redirect to appointments page (general)
                    return Url.Action("Manage", "Appointments");
                    // Option 2: Could redirect to patient details to see all their appointments
                    // return Url.Action("Details", "Precord", new { id = patientId });
                }
                return Url.Action("Manage", "Appointments");
            }
            else if (content.Contains("patient email verified", StringComparison.OrdinalIgnoreCase) ||
                     content.Contains("patient registered", StringComparison.OrdinalIgnoreCase))
            {
                // For patient notifications, redirect to patient management
                // If patient ID is available, go directly to patient details
                if (patientId > 0)
                {
                    return Url.Action("Details", "Precord", new { id = patientId });
                }
                return Url.Action("Index", "Precord");
            }
            else if (content.Contains("clinic staff email verified", StringComparison.OrdinalIgnoreCase) ||
                     content.Contains("clinic staff registered", StringComparison.OrdinalIgnoreCase))
            {
                // For staff notifications, redirect to clinic staff management
                return Url.Action("Index", "Clinicstaff");
            }
            else if (patientId > 0)
            {
                // If notification has a patient ID but content doesn't match known patterns,
                // redirect to patient details as default
                return Url.Action("Details", "Precord", new { id = patientId });
            }

            // Default: stay on dashboard
            return Url.Action("Index", "Dashboard");
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

    public class MarkEmergencyResolvedRequest
    {
        public int EmergencyId { get; set; }
    }

    public class CreateWalkInAppointmentRequest
    {
        public string StudentFullName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string AppointmentDate { get; set; } = string.Empty;
        public string AppointmentTime { get; set; } = string.Empty;
        public string ReasonForVisit { get; set; } = string.Empty;
        public string PriorityLevel { get; set; } = string.Empty;
    }

    public class MarkNotificationReadRequest
    {
        public int NotificationId { get; set; }
    }
}
