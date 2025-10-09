using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Controllers
{
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Notification
        public async Task<IActionResult> Index()
        {
            var notifications = _context.Notifications
                .Include(n => n.ClinicStaff)
                .Include(n => n.Patient);
            return View(await notifications.ToListAsync());
        }

        // GET: Notification/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var notification = await _context.Notifications
                .Include(n => n.ClinicStaff)
                .Include(n => n.Patient)
                .FirstOrDefaultAsync(m => m.NotificationId == id);

            if (notification == null)
                return NotFound();

            return View(notification);
        }

        // GET: Notification/Create
        public IActionResult Create()
        {
            ViewData["ClinicStaffId"] = new SelectList(_context.Clinicstaffs, "ClinicStaffId", "FirstName");
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName");
            return View();
        }

        // POST: Notification/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClinicStaffId,PatientId,Content,NotifDateTime,IsRead")] Notification notification)
        {
            if (ModelState.IsValid)
            {
                _context.Add(notification);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClinicStaffId"] = new SelectList(_context.Clinicstaffs, "ClinicStaffId", "FirstName", notification.ClinicStaffId);
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", notification.PatientId);
            return View(notification);
        }

        // GET: Notification/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            ViewData["ClinicStaffId"] = new SelectList(_context.Clinicstaffs, "ClinicStaffId", "FirstName", notification.ClinicStaffId);
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", notification.PatientId);
            return View(notification);
        }

        // POST: Notification/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("NotificationId,ClinicStaffId,PatientId,Content,NotifDateTime,IsRead")] Notification notification)
        {
            if (id != notification.NotificationId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(notification);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NotificationExists(notification.NotificationId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClinicStaffId"] = new SelectList(_context.Clinicstaffs, "ClinicStaffId", "FirstName", notification.ClinicStaffId);
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", notification.PatientId);
            return View(notification);
        }

        // GET: Notification/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var notification = await _context.Notifications
                .Include(n => n.ClinicStaff)
                .Include(n => n.Patient)
                .FirstOrDefaultAsync(m => m.NotificationId == id);

            if (notification == null)
                return NotFound();

            return View(notification);
        }

        // POST: Notification/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool NotificationExists(int id)
        {
            return _context.Notifications.Any(e => e.NotificationId == id);
        }
    }
}