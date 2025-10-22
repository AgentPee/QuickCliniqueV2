using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Controllers
{
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: History
        public async Task<IActionResult> Index()
        {
            var histories = _context.Histories
                .Include(h => h.Patient)
                .Include(h => h.Appointment)
                .Include(h => h.Schedule);

            var result = await histories.ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = result });

            return View(result);
        }

        // GET: History/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var history = await _context.Histories
                .Include(h => h.Patient)
                .Include(h => h.Appointment)
                .Include(h => h.Schedule)
                .FirstOrDefaultAsync(m => m.HistoryId == id);

            if (history == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "History not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = history });

            return View(history);
        }

        // GET: History/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName");
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit");
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date");

            if (IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    patients = ViewData["PatientId"],
                    appointments = ViewData["AppointmentId"],
                    schedules = ViewData["ScheduleId"]
                });

            return View();
        }

        // POST: History/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,AppointmentId,ScheduleId,VisitReason,Idnumber,Date")] History history)
        {
            if (ModelState.IsValid)
            {
                _context.Add(history);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "History created successfully", id = history.HistoryId });

                return RedirectToAction(nameof(Index));
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", history.PatientId);
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit", history.AppointmentId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", history.ScheduleId);

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

            return View(history);
        }

        // GET: History/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var history = await _context.Histories.FindAsync(id);
            if (history == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "History not found" });
                return NotFound();
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", history.PatientId);
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit", history.AppointmentId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", history.ScheduleId);

            if (IsAjaxRequest())
                return Json(new { success = true, data = history });

            return View(history);
        }

        // POST: History/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HistoryId,PatientId,AppointmentId,ScheduleId,VisitReason,Idnumber,Date")] History history)
        {
            if (id != history.HistoryId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(history);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "History updated successfully" });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HistoryExists(history.HistoryId))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "History not found" });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", history.PatientId);
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit", history.AppointmentId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", history.ScheduleId);

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

            return View(history);
        }

        // GET: History/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var history = await _context.Histories
                .Include(h => h.Patient)
                .Include(h => h.Appointment)
                .Include(h => h.Schedule)
                .FirstOrDefaultAsync(m => m.HistoryId == id);

            if (history == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "History not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = history });

            return View(history);
        }

        // POST: History/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var history = await _context.Histories.FindAsync(id);
            if (history != null)
            {
                _context.Histories.Remove(history);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "History deleted successfully" });
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "History not found" });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool HistoryExists(int id)
        {
            return _context.Histories.Any(e => e.HistoryId == id);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}