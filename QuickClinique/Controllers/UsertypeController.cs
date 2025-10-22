using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Controllers
{
    public class UsertypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsertypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Usertype
        public async Task<IActionResult> Index()
        {
            var result = await _context.Usertypes.ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = result });

            return View(result);
        }

        // GET: Usertype/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var usertype = await _context.Usertypes
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (usertype == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "User type not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = usertype });

            return View(usertype);
        }

        // GET: Usertype/Create
        public IActionResult Create()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Usertype/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Role,Name")] Usertype usertype)
        {
            if (ModelState.IsValid)
            {
                _context.Add(usertype);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "User type created successfully", id = usertype.UserId });

                return RedirectToAction(nameof(Index));
            }

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

            return View(usertype);
        }

        // GET: Usertype/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var usertype = await _context.Usertypes.FindAsync(id);
            if (usertype == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "User type not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = usertype });

            return View(usertype);
        }

        // POST: Usertype/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,Role,Name")] Usertype usertype)
        {
            if (id != usertype.UserId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(usertype);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "User type updated successfully" });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsertypeExists(usertype.UserId))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "User type not found" });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

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

            return View(usertype);
        }

        // GET: Usertype/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var usertype = await _context.Usertypes
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (usertype == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "User type not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = usertype });

            return View(usertype);
        }

        // POST: Usertype/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usertype = await _context.Usertypes.FindAsync(id);
            if (usertype != null)
            {
                _context.Usertypes.Remove(usertype);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "User type deleted successfully" });
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "User type not found" });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UsertypeExists(int id)
        {
            return _context.Usertypes.Any(e => e.UserId == id);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}