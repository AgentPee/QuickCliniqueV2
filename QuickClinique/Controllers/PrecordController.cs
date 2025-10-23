using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Attributes;

namespace QuickClinique.Controllers
{
    [ClinicStaffOnly] // Only clinic staff can access patient records
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
            var result = await precords.ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = result });

            return View(result);
        }

        // GET: Precord/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var precord = await _context.Precords
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.RecordId == id);

            if (precord == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Patient record not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = precord });

            return View(precord);
        }

        // GET: Precord/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName");

            if (IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    patients = ViewData["PatientId"]
                });

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

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Patient record created successfully", id = precord.RecordId });

                return RedirectToAction(nameof(Index));
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);

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

            return View(precord);
        }

        // GET: Precord/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var precord = await _context.Precords.FindAsync(id);
            if (precord == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Patient record not found" });
                return NotFound();
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);

            if (IsAjaxRequest())
                return Json(new { success = true, data = precord });

            return View(precord);
        }

        // POST: Precord/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RecordId,PatientId,Diagnosis,Medications,Allergies,Name,Age,Gender,Bmi")] Precord precord)
        {
            if (id != precord.RecordId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(precord);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Patient record updated successfully" });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrecordExists(precord.RecordId))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Patient record not found" });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);

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

            return View(precord);
        }

        // GET: Precord/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var precord = await _context.Precords
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.RecordId == id);

            if (precord == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Patient record not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = precord });

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

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Patient record deleted successfully" });
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Patient record not found" });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PrecordExists(int id)
        {
            return _context.Precords.Any(e => e.RecordId == id);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}