using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Controllers
{
    public class PrecordController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrecordController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Precord
        public async Task<IActionResult> Index()
        {
            var precords = _context.Precords
                .Include(p => p.Patient);
            return View(await precords.ToListAsync());
        }

        // GET: Precord/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var precord = await _context.Precords
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.RecordId == id);

            if (precord == null)
                return NotFound();

            return View(precord);
        }

        // GET: Precord/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName");
            return View();
        }

        // POST: Precord/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,Diagnosis,Medications,Allergies,Name,Age,Gender,Bmi")] Precord precord)
        {
            if (ModelState.IsValid)
            {
                _context.Add(precord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);
            return View(precord);
        }

        // GET: Precord/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var precord = await _context.Precords.FindAsync(id);
            if (precord == null)
                return NotFound();

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);
            return View(precord);
        }

        // POST: Precord/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecordId,PatientId,Diagnosis,Medications,Allergies,Name,Age,Gender,Bmi")] Precord precord)
        {
            if (id != precord.RecordId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(precord);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrecordExists(precord.RecordId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);
            return View(precord);
        }

        // GET: Precord/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var precord = await _context.Precords
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.RecordId == id);

            if (precord == null)
                return NotFound();

            return View(precord);
        }

        // POST: Precord/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var precord = await _context.Precords.FindAsync(id);
            if (precord != null)
            {
                _context.Precords.Remove(precord);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PrecordExists(int id)
        {
            return _context.Precords.Any(e => e.RecordId == id);
        }
    }
}