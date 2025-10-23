using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Attributes;

namespace QuickClinique.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Schedule)
                .ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = appointments });

            return View(appointments);
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

                var lastQueue = await _context.Appointments
                    .Where(a => a.ScheduleId == model.ScheduleId)
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
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,ScheduleId,AppointmentStatus,ReasonForVisit,DateBooked,QueueNumber,QueueStatus,Symptoms")] Appointment appointment)
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
        [ValidateAntiForgeryToken]
        [ClinicStaffOnly]
        public async Task<IActionResult> UpdateStatus(int appointmentId, string status)
        {
            try
            {
                var appointment = await _context.Appointments.FindAsync(appointmentId);
                if (appointment == null)
                {
                    return Json(new { success = false, error = "Appointment not found" });
                }

                // Validate status
                var validStatuses = new[] { "Pending", "Confirmed", "In Progress", "Completed", "Cancelled" };
                if (!validStatuses.Contains(status))
                {
                    return Json(new { success = false, error = "Invalid status" });
                }

                appointment.AppointmentStatus = status;

                // Update queue status based on appointment status
                switch (status)
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

                return Json(new { success = true, message = $"Appointment status updated to {status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Error updating appointment status" });
            }
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
                           (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed" || a.AppointmentStatus == "In Progress"))
                .OrderBy(a => a.QueueNumber)
                .ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = appointments });

            return View(appointments);
        }

        // POST: Appointments/NextInQueue - Move to next patient in queue
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ClinicStaffOnly]
        public async Task<IActionResult> NextInQueue()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                
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

                // Update current appointment to "In Progress"
                nextAppointment.AppointmentStatus = "In Progress";
                nextAppointment.QueueStatus = "Being Served";

                await _context.SaveChangesAsync();

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
    }
}