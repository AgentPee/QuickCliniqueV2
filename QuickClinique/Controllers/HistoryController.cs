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
            return View(await histories.ToListAsync());
        }

        // GET: History/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var history = await _context.Histories
                .Include(h => h.Patient)
                .Include(h => h.Appointment)
                .Include(h => h.Schedule)
                .FirstOrDefaultAsync(m => m.HistoryId == id);

            if (history == null)
                return NotFound();

            return View(history);
        }

        // GET: History/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName");
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit");
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date");
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", history.PatientId);
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit", history.AppointmentId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", history.ScheduleId);
            return View(history);
        }

        // GET: History/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var history = await _context.Histories.FindAsync(id);
            if (history == null)
                return NotFound();

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", history.PatientId);
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit", history.AppointmentId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", history.ScheduleId);
            return View(history);
        }

        // POST: History/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("HistoryId,PatientId,AppointmentId,ScheduleId,VisitReason,Idnumber,Date")] History history)
        {
            if (id != history.HistoryId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(history);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HistoryExists(history.HistoryId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", history.PatientId);
            ViewData["AppointmentId"] = new SelectList(_context.Appointments, "AppointmentId", "ReasonForVisit", history.AppointmentId);
            ViewData["ScheduleId"] = new SelectList(_context.Schedules, "ScheduleId", "Date", history.ScheduleId);
            return View(history);
        }

        // GET: History/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var history = await _context.Histories
                .Include(h => h.Patient)
                .Include(h => h.Appointment)
                .Include(h => h.Schedule)
                .FirstOrDefaultAsync(m => m.HistoryId == id);

            if (history == null)
                return NotFound();

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
            }
            return RedirectToAction(nameof(Index));
        }

        private bool HistoryExists(int id)
        {
            return _context.Histories.Any(e => e.HistoryId == id);
        }
    }
}