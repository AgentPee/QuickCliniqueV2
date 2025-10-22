using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

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
        public async Task<IActionResult> Create([Bind("PatientId,ScheduleId,ReasonForVisit,Symptoms")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                appointment.DateBooked = DateOnly.FromDateTime(DateTime.Now);
                appointment.AppointmentStatus = "Pending";
                appointment.QueueStatus = "Waiting";

                var lastQueue = await _context.Appointments
                    .Where(a => a.ScheduleId == appointment.ScheduleId)
                    .OrderByDescending(a => a.QueueNumber)
                    .FirstOrDefaultAsync();
                appointment.QueueNumber = (lastQueue?.QueueNumber ?? 0) + 1;

                _context.Add(appointment);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Appointment created successfully", id = appointment.AppointmentId });

                return RedirectToAction(nameof(Index));
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

        // GET: Appointments/Edit/5
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
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,ScheduleId,AppointmentStatus,ReasonForVisit,DateBooked,QueueNumber,QueueStatus")] Appointment appointment)
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

        // GET: Appointments/GetAvailableSlots
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
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        DisplayText = $"{s.Date:MMM dd, yyyy} - {s.StartTime:hh\\:mm} to {s.EndTime:hh\\:mm}",
                        AvailableAppointments = _context.Appointments.Count(a => a.ScheduleId == s.ScheduleId &&
                            (a.AppointmentStatus == "Pending" || a.AppointmentStatus == "Confirmed"))
                    })
                    .ToListAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, data = availableSlots });

                return Json(new { success = true, data = availableSlots });
            }
            catch (Exception ex)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Error retrieving available slots" });

                return Json(new { success = false, error = "Error retrieving available slots" });
            }
        }
    }
}