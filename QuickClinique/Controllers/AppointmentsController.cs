using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuickClinique.Models;
using QuickClinique.Attributes;
using QuickClinique.Services;

namespace QuickClinique.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AppointmentsController(ApplicationDbContext context, IEmailService emailService, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _emailService = emailService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Appointment not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = appointment });

            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FullName");
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date");

            if (IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    patients = ViewData["PatientId"],
                    schedules = ViewData["ScheduleId"]
                });

            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [StudentOnly] // Only students can book appointments
        public async Task<IActionResult> Create([FromForm] AppointmentCreateViewModel model)
        {
            // Debug logging
            Console.WriteLine($"Received appointment data:");
            Console.WriteLine($"PatientId: {model.PatientId}");
            Console.WriteLine($"ScheduleId: {model.ScheduleId}");
            Console.WriteLine($"ReasonForVisit: '{model.ReasonForVisit}'");
            Console.WriteLine($"Symptoms: '{model.Symptoms}'");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState errors:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Field: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            if (ModelState.IsValid)
            {
                // Get the schedule to find the appointment date
                var schedule = await _context.Schedules.FindAsync(model.ScheduleId);
                if (schedule == null)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Schedule not found" });
                    ModelState.AddModelError("ScheduleId", "Selected schedule is not available");
                    ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FullName", model.PatientId);
                    ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", model.ScheduleId);
                    return View(model);
                }

                // Create the appointment entity
                var appointment = new Appointment
                {
                    PatientId = model.PatientId,
                    ScheduleId = model.ScheduleId,
                    ReasonForVisit = model.ReasonForVisit,
                    Symptoms = string.IsNullOrWhiteSpace(model.Symptoms) ? "No symptoms provided" : model.Symptoms,
                    DateBooked = DateOnly.FromDateTime(DateTime.Now),
                    AppointmentStatus = "Pending",
                    QueueStatus = "Waiting"
                };

                // Assign queue number based on the appointment date (not schedule ID)
                // All appointments on the same day should share the same queue sequence
                var lastQueue = await _context.Appointments
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == schedule.Date)
                    .OrderByDescending(a => a.QueueNumber)
                    .FirstOrDefaultAsync();
                appointment.QueueNumber = (lastQueue?.QueueNumber ?? 0) + 1;

                _context.Add(appointment);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new
                    {
                        success = true,
                        message = "Appointment created successfully",
                        id = appointment.AppointmentId,
                        queueNumber = appointment.QueueNumber
                    });

                return RedirectToAction(nameof(Index));
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FullName", model.PatientId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", model.ScheduleId);

            if (IsAjaxRequest())
                return Json(new
                {
                    success = false,
                    error = "Validation failed",
                    errors = ModelState.ToDictionary(
                        k => k.Key,
                        v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });

            return View(model);
        }

        // GET: Appointments/Edit/5
        [ClinicStaffOnly] // Only clinic staff can edit appointments
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Appointment not found" });
                return NotFound();
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FullName", appointment.PatientId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", appointment.ScheduleId);

            if (IsAjaxRequest())
                return Json(new { success = true, data = appointment });

            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClinicStaffOnly] // Only clinic staff can edit appointments
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,ScheduleId,AppointmentStatus,ReasonForVisit,DateBooked,QueueNumber,QueueStatus,Symptoms,TriageNotes")] Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Appointment updated successfully" });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentId))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Appointment not found" });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FullName", appointment.PatientId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", appointment.ScheduleId);

            if (IsAjaxRequest())
                return Json(new
                {
                    success = false,
                    error = "Validation failed",
                    errors = ModelState.ToDictionary(
                        k => k.Key,
                        v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });

            return View(appointment);
        }

        // GET: Appointments/Delete/5
        [ClinicStaffOnly] // Only clinic staff can delete appointments
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Appointment not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = appointment });

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [ClinicStaffOnly] // Only clinic staff can delete appointments
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Appointment deleted successfully" });
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Appointment not found" });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id);
        }

        /// <summary>
        /// Calculates age from a birthdate
        /// </summary>
        /// <param name="birthdate">The birthdate to calculate age from</param>
        /// <returns>The age in years, or 0 if birthdate is null</returns>
        private static int CalculateAge(DateOnly? birthdate)
        {
            if (!birthdate.HasValue)
                return 0;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthdate.Value.Year;
            
            // If birthday hasn't occurred this year yet, subtract 1
            if (birthdate.Value > today.AddYears(-age))
                age--;

            return age;
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }

        // GET: Appointments/GetAvailableSlots - Fixed version
        [HttpGet]
        public async Task<IActionResult> GetAvailableSlots(DateOnly? date = null)
        {
            try
            {
                var query = _context.Schedules
                    .Where(s => s.IsAvailable == "Yes" && s.Date >= DateOnly.FromDateTime(DateTime.Now));

                if (date.HasValue)
                {
                    query = query.Where(s => s.Date == date.Value);
                }

                var availableSlots = await query
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.StartTime)
                    .Select(s => new
                    {
                        ScheduleId = s.ScheduleId,
                        Date = s.Date,
                        StartTime = s.StartTime, // Keep as TimeOnly
                        EndTime = s.EndTime,     // Keep as TimeOnly
                        StartTimeFormatted = s.StartTime.ToString("h:mm tt"), // Add formatted version
                        EndTimeFormatted = s.EndTime.ToString("h:mm tt"),     // Add formatted version
                        DisplayText = $"{s.StartTime:h:mm tt} to {s.EndTime:h:mm tt}",
                        AvailableAppointments = _context.Appointments.Count(a => a.ScheduleId == s.ScheduleId &&
                            (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed"))
                    })
                    .ToListAsync();

                // Debug logging
                Console.WriteLine($"Found {availableSlots.Count} available slots");
                foreach (var slot in availableSlots)
                {
                    Console.WriteLine($"Slot: {slot.ScheduleId}, Start: {slot.StartTime}, End: {slot.EndTime}, Formatted: {slot.StartTimeFormatted} - {slot.EndTimeFormatted}");
                }

                if (IsAjaxRequest())
                    return Json(new { success = true, data = availableSlots });

                return Json(new { success = true, data = availableSlots });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAvailableSlots: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Error retrieving available slots" });

                return Json(new { success = false, error = "Error retrieving available slots" });
            }
        }

        // GET: Appointments/GetAvailableDates
        [HttpGet]
        public async Task<IActionResult> GetAvailableDates()
        {
            try
            {
                var availableDates = await _context.Schedules
                    .Where(s => s.IsAvailable == "Yes" && s.Date >= DateOnly.FromDateTime(DateTime.Now))
                    .Select(s => s.Date)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToListAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, data = availableDates });

                return Json(new { success = true, data = availableDates });
            }
            catch (Exception ex)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Error retrieving available dates" });

                return Json(new { success = false, error = "Error retrieving available dates" });
            }
        }

        // GET: Appointments/Manage - Clinic staff appointment management
        [ClinicStaffOnly]
        public async Task<IActionResult> Manage()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .OrderByDescending(a => a.DateBooked)
                .ThenBy(a => a.QueueNumber)
                .ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = appointments });

            return View(appointments);
        }

        // POST: Appointments/UpdateStatus - Update appointment status
        [HttpPost]
        [ClinicStaffOnly]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                Console.WriteLine($"UpdateStatus called with AppointmentId: {request?.AppointmentId}, Status: {request?.Status}");
                
                if (request == null || request.AppointmentId <= 0)
                {
                    Console.WriteLine("Invalid request data");
                    return Json(new { success = false, error = "Invalid request data" });
                }

                var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
                if (appointment == null)
                {
                    Console.WriteLine($"Appointment not found with ID: {request.AppointmentId}");
                    return Json(new { success = false, error = "Appointment not found" });
                }

                // Validate status
                var validStatuses = new[] { "Pending", "Confirmed", "In Progress", "Completed", "Cancelled" };
                if (!validStatuses.Contains(request.Status))
                {
                    return Json(new { success = false, error = "Invalid status" });
                }

                appointment.AppointmentStatus = request.Status;

                // Load related data for email notifications and triage
                await _context.Entry(appointment).Reference(a => a.Patient).LoadAsync();
                await _context.Entry(appointment).Reference(a => a.Schedule).LoadAsync();

                // If starting appointment (In Progress), create Precord with triage data
                if (request.Status == "In Progress")
                {
                    // Check if a Precord already exists for this appointment (in case of re-start)
                    // We'll create a new one for each appointment start
                    
                    // Calculate age from Birthdate if available, otherwise use request.Age or 0
                    int age = CalculateAge(appointment.Patient?.Birthdate);
                    if (age == 0 && request.Age.HasValue)
                    {
                        age = request.Age.Value;
                    }
                    
                    // Get gender from patient if available, otherwise use request.Gender or default
                    string gender = appointment.Patient?.Gender ?? request.Gender ?? "Not specified";
                    
                    var medicalRecord = new Precord
                    {
                        PatientId = appointment.PatientId,
                        Diagnosis = "Triage in progress - Diagnosis pending",
                        Medications = "None",
                        Allergies = request.Allergies ?? "None",
                        Name = appointment.Patient?.FullName ?? "Unknown",
                        Age = age,
                        Gender = gender,
                        Bmi = request.Bmi.HasValue ? (int)request.Bmi.Value : 0
                    };

                    _context.Precords.Add(medicalRecord);
                    
                    // Save triage notes to TriageNotes field in Appointment
                    if (!string.IsNullOrWhiteSpace(request.TriageNotes))
                    {
                        appointment.TriageNotes = request.TriageNotes;
                    }
                }

                // Update queue status based on appointment status
                switch (request.Status)
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
                Console.WriteLine($"Successfully updated appointment {request.AppointmentId} to status {request.Status}");

                // Send email notifications based on status (fire-and-forget to avoid blocking response)
                Console.WriteLine($"[EMAIL DEBUG] Checking if email should be sent for appointment {appointment.AppointmentId}");
                Console.WriteLine($"[EMAIL DEBUG] Patient is null: {appointment.Patient == null}");
                if (appointment.Patient != null)
                {
                    Console.WriteLine($"[EMAIL DEBUG] Patient email: '{appointment.Patient.Email}'");
                    Console.WriteLine($"[EMAIL DEBUG] Patient email is empty: {string.IsNullOrEmpty(appointment.Patient.Email)}");
                }
                
                if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.Email))
                {
                    Console.WriteLine($"[EMAIL DEBUG] Preparing to send {request.Status} email to {appointment.Patient.Email}");
                    
                    // Capture values for closure
                    var patientEmail = appointment.Patient.Email;
                    var patientName = appointment.Patient.FullName;
                    var status = request.Status;
                    var schedule = appointment.Schedule;
                    var queueNumber = appointment.QueueNumber;
                    var cancellationReason = appointment.CancellationReason;
                    
                    // Fire-and-forget: don't await, let it run in background with proper scope
                    _ = Task.Run(async () =>
                    {
                        // Create a new scope for the background task
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            try
                            {
                                Console.WriteLine($"[EMAIL DEBUG] Task.Run started for {status} email to {patientEmail}");
                                
                                if (status == "Confirmed")
                                {
                                    var appointmentDate = schedule?.Date.ToString("MMM dd, yyyy") ?? "N/A";
                                    var appointmentTime = schedule != null 
                                        ? $"{schedule.StartTime:h:mm tt} - {schedule.EndTime:h:mm tt}"
                                        : "N/A";
                                    
                                    Console.WriteLine($"[EMAIL DEBUG] Sending confirmation email to {patientEmail}");
                                    await emailService.SendAppointmentConfirmationEmail(
                                        patientEmail,
                                        patientName,
                                        appointmentDate,
                                        appointmentTime,
                                        queueNumber
                                    );
                                    Console.WriteLine($"[EMAIL DEBUG] Confirmation email sent successfully to {patientEmail}");
                                }
                                else if (status == "Completed")
                                {
                                    var appointmentDate = schedule?.Date.ToString("MMM dd, yyyy") ?? DateTime.Now.ToString("MMM dd, yyyy");
                                    Console.WriteLine($"[EMAIL DEBUG] Sending completion email to {patientEmail}");
                                    await emailService.SendAppointmentCompletedEmail(
                                        patientEmail,
                                        patientName,
                                        appointmentDate
                                    );
                                    Console.WriteLine($"[EMAIL DEBUG] Completion email sent successfully to {patientEmail}");
                                }
                                else if (status == "Cancelled")
                                {
                                    var appointmentDate = schedule?.Date.ToString("MMM dd, yyyy") ?? "N/A";
                                    var appointmentTime = schedule != null 
                                        ? $"{schedule.StartTime:h:mm tt} - {schedule.EndTime:h:mm tt}"
                                        : "N/A";
                                    
                                    Console.WriteLine($"[EMAIL DEBUG] Sending cancellation email to {patientEmail}");
                                    await emailService.SendAppointmentCancellationEmail(
                                        patientEmail,
                                        patientName,
                                        appointmentDate,
                                        appointmentTime,
                                        cancellationReason
                                    );
                                    Console.WriteLine($"[EMAIL DEBUG] Cancellation email sent successfully to {patientEmail}");
                                }
                            }
                            catch (Exception emailEx)
                            {
                                // Log email error but don't fail the request
                                Console.WriteLine($"[EMAIL ERROR] Failed to send {status} email to {patientEmail}: {emailEx.Message}");
                                Console.WriteLine($"[EMAIL ERROR] Stack trace: {emailEx.StackTrace}");
                                if (emailEx.InnerException != null)
                                {
                                    Console.WriteLine($"[EMAIL ERROR] Inner exception: {emailEx.InnerException.Message}");
                                }
                            }
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[EMAIL DEBUG] Email NOT sent - Patient is null or email is empty");
                }

                return Json(new { success = true, message = $"Appointment status updated to {request.Status}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating appointment status: {ex.Message}");
                return Json(new { success = false, error = "Error updating appointment status" });
            }
        }

        // Helper class for UpdateStatus request
        public class UpdateStatusRequest
        {
            public int AppointmentId { get; set; }
            public required string Status { get; set; }
            // Optional triage data for starting appointments
            public int? Age { get; set; }
            public string? Gender { get; set; }
            public double? Bmi { get; set; }
            public string? Allergies { get; set; }
            public string? TriageNotes { get; set; }
        }

        // Helper class for NextInQueue request
        public class NextInQueueRequest
        {
            // Optional triage data for next patient - Vital Signs
            public int? PulseRate { get; set; }
            public string? BloodPressure { get; set; }
            public decimal? Temperature { get; set; }
            public int? RespiratoryRate { get; set; }
            public int? OxygenSaturation { get; set; }
            public double? Bmi { get; set; }
            public string? Allergies { get; set; }
            public string? TriageNotes { get; set; }
        }

        // GET: Appointments/Queue - Real-time queue management
        [ClinicStaffOnly]
        public async Task<IActionResult> Queue()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .Where(a => a.Schedule.Date == today && 
                           (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "In Progress") &&
                           a.QueueStatus != "Done")
                .OrderBy(a => a.QueueNumber)
                .ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = appointments });

            return View(appointments);
        }

        // GET: Appointments/GetQueueData - Get queue data for background refresh
        [HttpGet]
        [ClinicStaffOnly]
        public async Task<IActionResult> GetQueueData()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .Where(a => a.Schedule.Date == today && 
                           (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "In Progress" || a.AppointmentStatus == "Completed") &&
                           a.QueueStatus != "Done")
                .OrderBy(a => a.QueueNumber)
                .ToListAsync();

            var currentPatient = appointments.FirstOrDefault(a => a.AppointmentStatus == "In Progress");
            var waitingPatients = appointments.Where(a => a.AppointmentStatus == "Confirmed" && a.QueueStatus == "Waiting")
                .OrderBy(a => a.QueueNumber)
                .ToList();

            return Json(new { 
                success = true, 
                data = new {
                    stats = new {
                        totalWaiting = appointments.Count(a => a.AppointmentStatus == "Confirmed" && a.QueueStatus == "Waiting"),
                        inProgress = appointments.Count(a => a.AppointmentStatus == "In Progress"),
                        completedToday = appointments.Count(a => a.AppointmentStatus == "Completed")
                    },
                    currentPatient = currentPatient != null ? new {
                        appointmentId = currentPatient.AppointmentId,
                        patientId = currentPatient.PatientId,
                        patientName = currentPatient.Patient?.FullName,
                        queueNumber = currentPatient.QueueNumber,
                        hasWaitingPatients = waitingPatients.Any()
                    } : null,
                    waitingPatients = waitingPatients.Select(a => new {
                        appointmentId = a.AppointmentId,
                        patientName = a.Patient?.FullName,
                        queueNumber = a.QueueNumber,
                        reasonForVisit = a.ReasonForVisit,
                        startTime = a.Schedule?.StartTime.ToString("h:mm tt"),
                        endTime = a.Schedule?.EndTime.ToString("h:mm tt")
                    }).ToList()
                }
            });
        }

        // GET: Appointments/GetManageData - Get appointment management data for background refresh
        [HttpGet]
        [ClinicStaffOnly]
        public async Task<IActionResult> GetManageData()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .OrderByDescending(a => a.DateBooked)
                .ThenBy(a => a.QueueNumber)
                .ToListAsync();

            return Json(new { 
                success = true, 
                data = new {
                    stats = new {
                        totalAppointments = appointments.Count,
                        pendingAppointments = appointments.Count(a => a.AppointmentStatus == "Pending"),
                        confirmedAppointments = appointments.Count(a => a.AppointmentStatus == "Confirmed"),
                        todayAppointments = appointments.Count(a => a.Schedule.Date == DateOnly.FromDateTime(DateTime.Now))
                    },
                    appointments = appointments.Select(a => new {
                        appointmentId = a.AppointmentId,
                        patientId = a.PatientId,
                        patientName = a.Patient?.FullName,
                        scheduleDate = a.Schedule.Date.ToString("MMM dd, yyyy"),
                        startTime = a.Schedule.StartTime.ToString("h:mm tt"),
                        endTime = a.Schedule.EndTime.ToString("h:mm tt"),
                        appointmentStatus = a.AppointmentStatus,
                        queueNumber = a.QueueNumber,
                        queueStatus = a.QueueStatus,
                        reasonForVisit = a.ReasonForVisit,
                        symptoms = a.Symptoms,
                        dateBooked = a.DateBooked.ToString("MMM dd, yyyy")
                    }).ToList()
                }
            });
        }

        // POST: Appointments/NextInQueue - Move to next patient in queue
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClinicStaffOnly]
        public async Task<IActionResult> NextInQueue([FromBody] NextInQueueRequest? request = null)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                
                // First, move any current "In Progress" appointment to "Done" status
                var currentAppointment = await _context.Appointments
                    .Where(a => a.Schedule.Date == today && 
                               a.AppointmentStatus == "In Progress")
                    .FirstOrDefaultAsync();

                if (currentAppointment != null)
                {
                    // Mark current patient as done (they can complete medical record separately)
                    currentAppointment.QueueStatus = "Done";
                    // Keep status as "In Progress" until medical record is completed
                }
                
                // Find the next appointment in queue
                var nextAppointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == today && 
                               a.AppointmentStatus == "Confirmed" && 
                               a.QueueStatus == "Waiting")
                    .OrderBy(a => a.QueueNumber)
                    .FirstOrDefaultAsync();

                if (nextAppointment == null)
                {
                    return Json(new { success = false, message = "No patients waiting in queue" });
                }

                // Update next appointment to "In Progress"
                nextAppointment.AppointmentStatus = "In Progress";
                nextAppointment.QueueStatus = "Being Served";

                // Create Precord with triage data if provided
                if (request != null && 
                    (request.PulseRate.HasValue || request.BloodPressure != null || 
                     request.Temperature.HasValue || request.RespiratoryRate.HasValue ||
                     request.OxygenSaturation.HasValue || request.Bmi.HasValue || request.Allergies != null))
                {
                    // Calculate age from Birthdate if available
                    int age = CalculateAge(nextAppointment.Patient?.Birthdate);
                    
                    // Get gender from patient if available
                    string gender = nextAppointment.Patient?.Gender ?? "Not specified";
                    
                    var medicalRecord = new Precord
                    {
                        PatientId = nextAppointment.PatientId,
                        Diagnosis = "Triage in progress - Diagnosis pending",
                        Medications = "None",
                        Allergies = request.Allergies ?? "None",
                        Name = nextAppointment.Patient?.FullName ?? "Unknown",
                        Age = age,
                        Gender = gender,
                        Bmi = request.Bmi.HasValue ? (int)request.Bmi.Value : 0,
                        PulseRate = request.PulseRate,
                        BloodPressure = request.BloodPressure,
                        Temperature = request.Temperature,
                        RespiratoryRate = request.RespiratoryRate,
                        OxygenSaturation = request.OxygenSaturation
                    };

                    _context.Precords.Add(medicalRecord);
                    
                    // Save triage notes to TriageNotes field in Appointment
                    if (!string.IsNullOrWhiteSpace(request.TriageNotes))
                    {
                        nextAppointment.TriageNotes = request.TriageNotes;
                    }
                }

                await _context.SaveChangesAsync();

                // Notify all waiting patients that their queue position has moved up
                var waitingAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.Schedule.Date == today && 
                               a.AppointmentStatus == "Confirmed" && 
                               a.QueueStatus == "Waiting" &&
                               a.AppointmentId != nextAppointment.AppointmentId)
                    .OrderBy(a => a.QueueNumber)
                    .ToListAsync();

                // Calculate new positions and send emails (fire-and-forget to avoid blocking response)
                int position = 1;
                foreach (var waitingAppointment in waitingAppointments)
                {
                    if (waitingAppointment.Patient != null && !string.IsNullOrEmpty(waitingAppointment.Patient.Email))
                    {
                        var currentPosition = position; // Capture for closure
                        var currentQueueNumber = waitingAppointment.QueueNumber;
                        var patientEmail = waitingAppointment.Patient.Email;
                        var patientName = waitingAppointment.Patient.FullName;
                        
                        // Fire-and-forget: don't await, let it run in background with proper scope
                        _ = Task.Run(async () =>
                        {
                            // Create a new scope for the background task
                            using (var scope = _serviceScopeFactory.CreateScope())
                            {
                                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                try
                                {
                                    Console.WriteLine($"[EMAIL DEBUG] Sending queue position update email to {patientEmail}, position: {currentPosition}");
                                    await emailService.SendQueuePositionUpdateEmail(
                                        patientEmail,
                                        patientName,
                                        currentPosition,
                                        currentQueueNumber
                                    );
                                    Console.WriteLine($"[EMAIL DEBUG] Queue position update email sent successfully to {patientEmail}");
                                }
                                catch (Exception emailEx)
                                {
                                    // Log email error but don't fail the request
                                    Console.WriteLine($"[EMAIL ERROR] Failed to send queue position update email to {patientEmail}: {emailEx.Message}");
                                    Console.WriteLine($"[EMAIL ERROR] Stack trace: {emailEx.StackTrace}");
                                    if (emailEx.InnerException != null)
                                    {
                                        Console.WriteLine($"[EMAIL ERROR] Inner exception: {emailEx.InnerException.Message}");
                                    }
                                }
                            }
                        });
                    }
                    position++;
                }

                return Json(new { 
                    success = true, 
                    message = $"Now serving: {nextAppointment.Patient.FullName}",
                    appointment = new {
                        appointmentId = nextAppointment.AppointmentId,
                        patientName = nextAppointment.Patient.FullName,
                        queueNumber = nextAppointment.QueueNumber,
                        reason = nextAppointment.ReasonForVisit
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error moving to next patient" });
            }
        }

        // GET: Appointments/GetStudentAppointments
        [HttpGet]
        public async Task<IActionResult> GetStudentAppointments(int studentId)
        {
            try
            {
                var appointments = await _context.Appointments
                    .Include(a => a.Schedule)
                    .Where(a => a.PatientId == studentId)
                    .OrderByDescending(a => a.DateBooked)
                    .Select(a => new
                    {
                        appointmentId = a.AppointmentId,
                        patientId = a.PatientId,
                        scheduleId = a.ScheduleId,
                        appointmentStatus = a.AppointmentStatus,
                        reasonForVisit = a.ReasonForVisit,
                        symptoms = a.Symptoms,
                        dateBooked = a.DateBooked,
                        queueNumber = a.QueueNumber,
                        queueStatus = a.QueueStatus,
                        schedule = a.Schedule != null ? new
                        {
                            date = a.Schedule.Date,
                            startTime = a.Schedule.StartTime.ToString(),
                            endTime = a.Schedule.EndTime.ToString(),
                            isAvailable = a.Schedule.IsAvailable
                        } : null
                    })
                    .ToListAsync();

                return Json(new { success = true, data = appointments });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Failed to load appointments" });
            }
        }

        // POST: Appointments/PatientCancelAppointment - Patient-facing cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientCancelAppointment([FromBody] PatientCancelRequest request)
        {
            try
            {
                if (request == null || request.AppointmentId <= 0)
                {
                    return Json(new { success = false, error = "Invalid request data" });
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.AppointmentId == request.AppointmentId);

                if (appointment == null)
                {
                    return Json(new { success = false, error = "Appointment not found" });
                }

                // Verify patient ownership (optional - you may want to add session check)
                // var studentId = HttpContext.Session.GetInt32("StudentId");
                // if (studentId == null || appointment.PatientId != studentId)
                // {
                //     return Json(new { success = false, error = "Unauthorized" });
                // }

                if (appointment.AppointmentStatus == "Cancelled")
                {
                    return Json(new { success = false, error = "Appointment is already cancelled" });
                }

                if (appointment.AppointmentStatus == "Completed")
                {
                    return Json(new { success = false, error = "Cannot cancel a completed appointment" });
                }

                if (appointment.AppointmentStatus == "In Progress")
                {
                    return Json(new { success = false, error = "Cannot cancel an appointment that is in progress" });
                }

                appointment.AppointmentStatus = "Cancelled";
                appointment.QueueStatus = "Cancelled";
                appointment.CancellationReason = "Cancelled by patient";

                await _context.SaveChangesAsync();

                // Load schedule for email notification
                await _context.Entry(appointment).Reference(a => a.Schedule).LoadAsync();

                // Send cancellation email (fire-and-forget to avoid blocking response)
                Console.WriteLine($"[EMAIL DEBUG] PatientCancelAppointment - Checking if email should be sent for appointment {request.AppointmentId}");
                if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.Email))
                {
                    Console.WriteLine($"[EMAIL DEBUG] PatientCancelAppointment - Preparing to send cancellation email to {appointment.Patient.Email}");
                    var appointmentDate = appointment.Schedule?.Date.ToString("MMM dd, yyyy") ?? "N/A";
                    var appointmentTime = appointment.Schedule != null 
                        ? $"{appointment.Schedule.StartTime:h:mm tt} - {appointment.Schedule.EndTime:h:mm tt}"
                        : "N/A";
                    var patientEmail = appointment.Patient.Email;
                    var patientName = appointment.Patient.FullName;
                    var cancellationReason = appointment.CancellationReason;
                    
                    // Fire-and-forget: don't await, let it run in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            Console.WriteLine($"[EMAIL DEBUG] PatientCancelAppointment - Task.Run started, sending email to {patientEmail}");
                            await _emailService.SendAppointmentCancellationEmail(
                                patientEmail,
                                patientName,
                                appointmentDate,
                                appointmentTime,
                                cancellationReason
                            );
                            Console.WriteLine($"[EMAIL DEBUG] PatientCancelAppointment - Email sent successfully to {patientEmail}");
                        }
                        catch (Exception emailEx)
                        {
                            // Log email error but don't fail the request
                            Console.WriteLine($"[EMAIL ERROR] PatientCancelAppointment - Failed to send cancellation email to {patientEmail}: {emailEx.Message}");
                            Console.WriteLine($"[EMAIL ERROR] Stack trace: {emailEx.StackTrace}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[EMAIL DEBUG] PatientCancelAppointment - Email NOT sent - Patient is null or email is empty");
                }

                return Json(new { success = true, message = "Appointment cancelled successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Failed to cancel appointment" });
            }
        }

        // POST: Appointments/PatientRescheduleAppointment - Patient-facing reschedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientRescheduleAppointment([FromBody] PatientRescheduleRequest request)
        {
            try
            {
                if (request == null || request.AppointmentId <= 0 || string.IsNullOrEmpty(request.NewDate))
                {
                    return Json(new { success = false, error = "Invalid request data" });
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .FirstOrDefaultAsync(a => a.AppointmentId == request.AppointmentId);

                if (appointment == null)
                {
                    return Json(new { success = false, error = "Appointment not found" });
                }

                // Verify patient ownership (optional)
                // var studentId = HttpContext.Session.GetInt32("StudentId");
                // if (studentId == null || appointment.PatientId != studentId)
                // {
                //     return Json(new { success = false, error = "Unauthorized" });
                // }

                if (appointment.AppointmentStatus == "Completed")
                {
                    return Json(new { success = false, error = "Cannot reschedule a completed appointment" });
                }

                if (appointment.AppointmentStatus == "In Progress")
                {
                    return Json(new { success = false, error = "Cannot reschedule an appointment that is in progress" });
                }

                // Parse the new date
                if (!DateOnly.TryParse(request.NewDate, out DateOnly newDate))
                {
                    return Json(new { success = false, error = "Invalid date format" });
                }

                // Check if date is in the future
                if (newDate <= DateOnly.FromDateTime(DateTime.Today))
                {
                    return Json(new { success = false, error = "Please select a future date" });
                }

                // Find available schedule for the new date
                var availableSchedule = await _context.Schedules
                    .Where(s => s.Date == newDate && s.IsAvailable == "Yes")
                    .OrderBy(s => s.StartTime)
                    .FirstOrDefaultAsync();

                if (availableSchedule == null)
                {
                    return Json(new { success = false, error = "No available slots for the selected date. Please choose another date." });
                }

                // Update appointment to pending status and assign new schedule
                appointment.AppointmentStatus = "Pending";
                appointment.QueueStatus = "Pending";
                appointment.ScheduleId = availableSchedule.ScheduleId;
                appointment.QueueNumber = 0; // Reset queue number

                // Mark old schedule as available
                if (appointment.Schedule != null)
                {
                    appointment.Schedule.IsAvailable = "Yes";
                }

                // Mark new schedule as unavailable
                availableSchedule.IsAvailable = "No";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Appointment rescheduled successfully. Please wait for confirmation." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Failed to reschedule appointment" });
            }
        }

        // Helper classes for patient requests
        public class PatientCancelRequest
        {
            public int AppointmentId { get; set; }
        }

        public class PatientRescheduleRequest
        {
            public int AppointmentId { get; set; }
            public string NewDate { get; set; } = string.Empty;
        }

        // POST: Appointments/ConfirmAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClinicStaffOnly]
        public async Task<IActionResult> ConfirmAppointment(int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
                
                if (appointment == null)
                {
                    return Json(new { success = false, error = "Appointment not found" });
                }

                if (appointment.AppointmentStatus != "Pending")
                {
                    return Json(new { success = false, error = "Only pending appointments can be confirmed" });
                }

                appointment.AppointmentStatus = "Confirmed";
                appointment.QueueStatus = "Waiting";
                
                await _context.SaveChangesAsync();

                // Send confirmation email (fire-and-forget to avoid blocking response)
                Console.WriteLine($"[EMAIL DEBUG] ConfirmAppointment - Checking if email should be sent for appointment {appointmentId}");
                if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.Email))
                {
                    Console.WriteLine($"[EMAIL DEBUG] ConfirmAppointment - Preparing to send confirmation email to {appointment.Patient.Email}");
                    var appointmentDate = appointment.Schedule?.Date.ToString("MMM dd, yyyy") ?? "N/A";
                    var appointmentTime = appointment.Schedule != null 
                        ? $"{appointment.Schedule.StartTime:h:mm tt} - {appointment.Schedule.EndTime:h:mm tt}"
                        : "N/A";
                    
                    var patientEmail = appointment.Patient.Email;
                    var patientName = appointment.Patient.FullName;
                    var queueNumber = appointment.QueueNumber;
                    
                    // Fire-and-forget: don't await, let it run in background with proper scope
                    _ = Task.Run(async () =>
                    {
                        // Create a new scope for the background task
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            try
                            {
                                Console.WriteLine($"[EMAIL DEBUG] ConfirmAppointment - Task.Run started, sending email to {patientEmail}");
                                await emailService.SendAppointmentConfirmationEmail(
                                    patientEmail,
                                    patientName,
                                    appointmentDate,
                                    appointmentTime,
                                    queueNumber
                                );
                                Console.WriteLine($"[EMAIL DEBUG] ConfirmAppointment - Email sent successfully to {patientEmail}");
                            }
                            catch (Exception emailEx)
                            {
                                // Log email error but don't fail the request
                                Console.WriteLine($"[EMAIL ERROR] ConfirmAppointment - Failed to send confirmation email to {patientEmail}: {emailEx.Message}");
                                Console.WriteLine($"[EMAIL ERROR] Stack trace: {emailEx.StackTrace}");
                                if (emailEx.InnerException != null)
                                {
                                    Console.WriteLine($"[EMAIL ERROR] Inner exception: {emailEx.InnerException.Message}");
                                }
                            }
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[EMAIL DEBUG] ConfirmAppointment - Email NOT sent - Patient is null or email is empty");
                    if (appointment.Patient == null)
                    {
                        Console.WriteLine($"[EMAIL DEBUG] ConfirmAppointment - Patient is null");
                    }
                    else if (string.IsNullOrEmpty(appointment.Patient.Email))
                    {
                        Console.WriteLine($"[EMAIL DEBUG] ConfirmAppointment - Patient email is null or empty");
                    }
                }

                return Json(new { success = true, message = "Appointment confirmed successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Failed to confirm appointment" });
            }
        }

        // POST: Appointments/CancelAppointment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClinicStaffOnly]
        public async Task<IActionResult> CancelAppointment(int appointmentId, string? reason = null)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(appointmentId);
                
                if (appointment == null)
                {
                    return Json(new { success = false, error = "Appointment not found" });
                }

                if (appointment.AppointmentStatus == "Cancelled")
                {
                    return Json(new { success = false, error = "Appointment is already cancelled" });
                }

                if (appointment.AppointmentStatus == "Completed")
                {
                    return Json(new { success = false, error = "Cannot cancel a completed appointment" });
                }

                appointment.AppointmentStatus = "Cancelled";
                appointment.QueueStatus = "Cancelled";
                
                // Store the cancellation reason in the dedicated field
                appointment.CancellationReason = !string.IsNullOrEmpty(reason) ? reason : "Cancelled by clinic staff";
                
                await _context.SaveChangesAsync();

                // Load related data for email notification
                await _context.Entry(appointment).Reference(a => a.Patient).LoadAsync();
                await _context.Entry(appointment).Reference(a => a.Schedule).LoadAsync();

                // Send cancellation email (fire-and-forget to avoid blocking response)
                if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.Email))
                {
                    var appointmentDate = appointment.Schedule?.Date.ToString("MMM dd, yyyy") ?? "N/A";
                    var appointmentTime = appointment.Schedule != null 
                        ? $"{appointment.Schedule.StartTime:h:mm tt} - {appointment.Schedule.EndTime:h:mm tt}"
                        : "N/A";
                    var patientEmail = appointment.Patient.Email;
                    var patientName = appointment.Patient.FullName;
                    var cancellationReason = appointment.CancellationReason;
                    
                    // Fire-and-forget: don't await, let it run in background with proper scope
                    _ = Task.Run(async () =>
                    {
                        // Create a new scope for the background task
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            try
                            {
                                Console.WriteLine($"[EMAIL DEBUG] CancelAppointment - Sending cancellation email to {patientEmail}");
                                await emailService.SendAppointmentCancellationEmail(
                                    patientEmail,
                                    patientName,
                                    appointmentDate,
                                    appointmentTime,
                                    cancellationReason
                                );
                                Console.WriteLine($"[EMAIL DEBUG] CancelAppointment - Cancellation email sent successfully to {patientEmail}");
                            }
                            catch (Exception emailEx)
                            {
                                // Log email error but don't fail the request
                                Console.WriteLine($"[EMAIL ERROR] CancelAppointment - Failed to send cancellation email to {patientEmail}: {emailEx.Message}");
                                Console.WriteLine($"[EMAIL ERROR] Stack trace: {emailEx.StackTrace}");
                                if (emailEx.InnerException != null)
                                {
                                    Console.WriteLine($"[EMAIL ERROR] Inner exception: {emailEx.InnerException.Message}");
                                }
                            }
                        }
                    });
                }

                return Json(new { success = true, message = "Appointment cancelled successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Failed to cancel appointment" });
            }
        }

        // POST: Appointments/CompleteAppointment - Complete appointment with medical record creation
        [HttpPost]
        [ClinicStaffOnly]
        public async Task<IActionResult> CompleteAppointment([FromBody] CompleteAppointmentViewModel model)
        {
            try
            {
                Console.WriteLine($"CompleteAppointment called with AppointmentId: {model.AppointmentId}, PatientId: {model.PatientId}");
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    Console.WriteLine($"ModelState errors: {string.Join(", ", errors)}");
                    return Json(new { success = false, error = "Invalid data provided", details = errors });
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);
                
                if (appointment == null)
                {
                    Console.WriteLine($"Appointment not found with ID: {model.AppointmentId}");
                    return Json(new { success = false, error = "Appointment not found" });
                }

                if (appointment.AppointmentStatus != "In Progress")
                {
                    return Json(new { success = false, error = "Only appointments in progress can be completed" });
                }

                // Update appointment status
                appointment.AppointmentStatus = "Completed";
                appointment.QueueStatus = "Completed";

                // Get patient info from existing Precord (from triage) or use defaults
                // Look for the most recent Precord with placeholder diagnosis (created during triage)
                var existingPrecord = await _context.Precords
                    .Where(p => p.PatientId == model.PatientId && 
                               p.Diagnosis == "Triage in progress - Diagnosis pending")
                    .OrderByDescending(p => p.RecordId)
                    .FirstOrDefaultAsync();
                
                // If not found, get the most recent Precord for this patient
                if (existingPrecord == null)
                {
                    existingPrecord = await _context.Precords
                        .Where(p => p.PatientId == model.PatientId)
                        .OrderByDescending(p => p.RecordId)
                        .FirstOrDefaultAsync();
                }

                // Calculate age from Birthdate if available
                int age = CalculateAge(appointment.Patient?.Birthdate);
                
                // Get gender from patient if available
                string gender = appointment.Patient?.Gender ?? "Not specified";
                
                // Create or update medical record (Precord)
                var medicalRecord = existingPrecord != null 
                    ? existingPrecord // Update existing record from triage
                    : new Precord // Create new if none exists
                    {
                        PatientId = model.PatientId,
                        Name = appointment.Patient?.FullName ?? "Unknown",
                        Age = age,
                        Gender = gender,
                        Bmi = 0,
                        Allergies = "None"
                    };

                // Update diagnosis and medications
                medicalRecord.Diagnosis = model.Diagnosis;
                medicalRecord.Medications = model.Medications;
                
                // Update age and gender if not already set from triage
                if (existingPrecord == null || existingPrecord.Age == 0)
                {
                    medicalRecord.Age = age;
                }
                if (existingPrecord == null || string.IsNullOrWhiteSpace(existingPrecord.Gender) || existingPrecord.Gender == "Not specified")
                {
                    medicalRecord.Gender = gender;
                }

                // Only add if creating new record (not updating existing)
                if (existingPrecord == null)
                {
                    _context.Precords.Add(medicalRecord);
                }

                await _context.SaveChangesAsync();

                // Create history record
                var historyRecord = new History
                {
                    PatientId = model.PatientId,
                    AppointmentId = appointment.AppointmentId,
                    ScheduleId = appointment.ScheduleId,
                    VisitReason = appointment.ReasonForVisit,
                    Idnumber = appointment.Patient?.Idnumber ?? 0,
                    Date = DateOnly.FromDateTime(DateTime.Now)
                };

                _context.Histories.Add(historyRecord);
                await _context.SaveChangesAsync();

                // Send completion email (fire-and-forget to avoid blocking response)
                Console.WriteLine($"[EMAIL DEBUG] CompleteAppointment - Checking if email should be sent for appointment {model.AppointmentId}");
                if (appointment.Patient != null && !string.IsNullOrEmpty(appointment.Patient.Email))
                {
                    Console.WriteLine($"[EMAIL DEBUG] CompleteAppointment - Preparing to send completion email to {appointment.Patient.Email}");
                    var appointmentDate = appointment.Schedule?.Date.ToString("MMM dd, yyyy") ?? DateTime.Now.ToString("MMM dd, yyyy");
                    var patientEmail = appointment.Patient.Email;
                    var patientName = appointment.Patient.FullName;
                    
                    // Fire-and-forget: don't await, let it run in background with proper scope
                    _ = Task.Run(async () =>
                    {
                        // Create a new scope for the background task
                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                            try
                            {
                                Console.WriteLine($"[EMAIL DEBUG] CompleteAppointment - Task.Run started, sending email to {patientEmail}");
                                await emailService.SendAppointmentCompletedEmail(
                                    patientEmail,
                                    patientName,
                                    appointmentDate
                                );
                                Console.WriteLine($"[EMAIL DEBUG] CompleteAppointment - Email sent successfully to {patientEmail}");
                            }
                            catch (Exception emailEx)
                            {
                                // Log email error but don't fail the request
                                Console.WriteLine($"[EMAIL ERROR] CompleteAppointment - Failed to send completion email to {patientEmail}: {emailEx.Message}");
                                Console.WriteLine($"[EMAIL ERROR] Stack trace: {emailEx.StackTrace}");
                                if (emailEx.InnerException != null)
                                {
                                    Console.WriteLine($"[EMAIL ERROR] Inner exception: {emailEx.InnerException.Message}");
                                }
                            }
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"[EMAIL DEBUG] CompleteAppointment - Email NOT sent - Patient is null or email is empty");
                }

                return Json(new 
                { 
                    success = true, 
                    message = "Appointment completed successfully",
                    medicalRecordId = medicalRecord.RecordId,
                    historyRecordId = historyRecord.HistoryId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error completing appointment: {ex.Message}");
                return Json(new { success = false, error = "Failed to complete appointment" });
            }
        }

        // GET: Appointments/GetQueueStatus - Get real-time queue status for students
        [HttpGet]
        public async Task<IActionResult> GetQueueStatus(int? studentId = null)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                
                // Get all today's appointments in queue
                var todayAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.Schedule.Date == today && 
                               (a.AppointmentStatus == "Pending" || 
                                a.AppointmentStatus == "Confirmed" || 
                                a.AppointmentStatus == "In Progress"))
                    .OrderBy(a => a.QueueNumber)
                    .ToListAsync();

                // Get currently being served
                var nowServing = todayAppointments
                    .FirstOrDefault(a => a.QueueStatus == "Being Served");

                // Get waiting count
                var waitingCount = todayAppointments
                    .Count(a => a.QueueStatus == "Waiting");

                // Get student's position if studentId provided
                int? userQueueNumber = null;
                string? userQueueStatus = null;
                int? userPosition = null;
                int? estimatedWaitTime = null;

                if (studentId.HasValue)
                {
                    var userAppointment = todayAppointments
                        .FirstOrDefault(a => a.PatientId == studentId.Value);

                    if (userAppointment != null)
                    {
                        userQueueNumber = userAppointment.QueueNumber;
                        userQueueStatus = userAppointment.QueueStatus;
                        
                        // Calculate position in queue (only count waiting appointments before this one)
                        if (userAppointment.QueueStatus == "Waiting")
                        {
                            userPosition = todayAppointments
                                .Count(a => a.QueueStatus == "Waiting" && 
                                           a.QueueNumber < userAppointment.QueueNumber) + 1;
                            
                            // Calculate estimated wait time: 15 minutes for first in line, +5 minutes for each subsequent person
                            // Formula: 15 + (position - 1) * 5
                            estimatedWaitTime = 15 + (userPosition.Value - 1) * 15;
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        nowServing = nowServing != null ? new
                        {
                            queueNumber = nowServing.QueueNumber,
                            patientName = nowServing.Patient?.FullName ?? "Unknown",
                            service = nowServing.ReasonForVisit
                        } : null,
                        waitingCount = waitingCount,
                        totalInQueue = todayAppointments.Count,
                        userQueueNumber = userQueueNumber,
                        userQueueStatus = userQueueStatus,
                        userPosition = userPosition,
                        estimatedWaitTime = estimatedWaitTime,
                        lastUpdated = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching queue status: {ex.Message}");
                return Json(new { success = false, error = "Failed to fetch queue status" });
            }
        }
    }
}