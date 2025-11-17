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

        // GET: Precord - Show all patients registered in the system
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = students });

            return View(students);
        }

        // GET: Precord/Details/5 - Show patient details with appointment history
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Appointments)
                    .ThenInclude(a => a.Schedule)
                .Include(s => s.Precords)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Patient not found" });
                return NotFound();
            }

            // Order appointments by date descending (most recent first)
            var orderedAppointments = student.Appointments
                .OrderByDescending(a => a.Schedule.Date)
                .ThenByDescending(a => a.Schedule.StartTime)
                .ToList();
            
            student.Appointments = orderedAppointments;

            if (IsAjaxRequest())
                return Json(new { success = true, data = student });

            return View(student);
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

        // GET: Precord/EditPatient/5 - Edit patient (Student) information
        [HttpGet]
        public async Task<IActionResult> EditPatient(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            return PartialView("_EditPatientForm", student);
        }

        // POST: Precord/EditPatient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(int id)
        {
            // Get the existing student first
            var existingStudent = await _context.Students.FindAsync(id);
            if (existingStudent == null)
            {
                return Json(new { success = false, message = "Patient not found" });
            }

            // Get form values manually to avoid model binding issues
            var idnumber = Request.Form["Idnumber"];
            var firstName = Request.Form["FirstName"];
            var lastName = Request.Form["LastName"];
            var email = Request.Form["Email"];
            var phoneNumber = Request.Form["PhoneNumber"];

            // Validate required fields
            if (string.IsNullOrWhiteSpace(firstName))
            {
                return Json(new { success = false, message = "First Name is required" });
            }
            if (string.IsNullOrWhiteSpace(lastName))
            {
                return Json(new { success = false, message = "Last Name is required" });
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Email is required" });
            }
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Json(new { success = false, message = "Phone Number is required" });
            }
            if (!int.TryParse(idnumber, out int parsedIdnumber))
            {
                return Json(new { success = false, message = "ID Number must be a valid number" });
            }

            try
            {
                // Update only the fields we want to change
                existingStudent.Idnumber = parsedIdnumber;
                existingStudent.FirstName = firstName.ToString().Trim();
                existingStudent.LastName = lastName.ToString().Trim();
                existingStudent.Email = email.ToString().Trim();
                existingStudent.PhoneNumber = phoneNumber.ToString().Trim();

                _context.Update(existingStudent);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Patient information updated successfully" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Students.Any(e => e.StudentId == id))
                {
                    return Json(new { success = false, message = "Patient not found" });
                }
                else
                {
                    return Json(new { success = false, message = "An error occurred while updating. Please try again." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // POST: Precord/ToggleActivePatient/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivePatient(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return Json(new { success = false, message = "Patient not found" });
            }

            try
            {
                // Toggle the IsActive status
                student.IsActive = !student.IsActive;
                await _context.SaveChangesAsync();

                string action = student.IsActive ? "activated" : "deactivated";
                return Json(new { success = true, message = $"Patient {action} successfully", isActive = student.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating patient status: {ex.Message}" });
            }
        }

    }
}