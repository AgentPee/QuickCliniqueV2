using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Attributes;

namespace QuickClinique.Controllers
{
    [ClinicStaffOnly] // Only clinic staff can access patient records
    public class PrecordController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public PrecordController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Helper method to get the base URL for absolute links (for email verification, etc.)
        private string GetBaseUrl()
        {
            // Check for BASE_URL environment variable first (for Railway/production)
            var baseUrl = Environment.GetEnvironmentVariable("BASE_URL");
            if (!string.IsNullOrEmpty(baseUrl))
            {
                return baseUrl.TrimEnd('/');
            }

            // Fall back to using the request's scheme and host
            var scheme = Request.Scheme;
            var host = Request.Host.Value;
            return $"{scheme}://{host}";
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
                try
                {
                    // Verify patient exists and get patient data
                    var patient = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == precord.PatientId);
                    if (patient == null)
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Selected patient not found. Please select a valid patient." });

                        ModelState.AddModelError("PatientId", "Selected patient not found. Please select a valid patient.");
                        ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);
                        return View(precord);
                    }

                    // Populate age from Birthdate if available and age is 0
                    if (precord.Age == 0)
                    {
                        precord.Age = CalculateAge(patient.Birthdate);
                    }
                    
                    // Populate gender from patient if available and gender is not set or is default
                    if ((string.IsNullOrWhiteSpace(precord.Gender) || precord.Gender == "Not specified") && !string.IsNullOrWhiteSpace(patient.Gender))
                    {
                        precord.Gender = patient.Gender;
                    }

                    _context.Add(precord);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Patient record created successfully", id = precord.RecordId });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"Database error creating patient record: {dbEx.Message}");
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred while saving the patient record. Please try again." });

                    ModelState.AddModelError("", "An error occurred while saving the patient record. Please try again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating patient record: {ex.Message}");
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An unexpected error occurred. Please try again." });

                    ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                }
            }

            ViewData["PatientId"] = new SelectList(_context.Students, "StudentId", "FirstName", precord.PatientId);

            if (IsAjaxRequest())
                return Json(new
                {
                    success = false,
                    error = "Please correct the validation errors and try again.",
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

        /// <summary>
        /// Calculates age from a birthdate
        /// </summary>
        /// <param name="birthdate">The birthdate to calculate age from</param>
        /// <returns>The age in years, or 0 if birthdate is null or invalid</returns>
        private static int CalculateAge(DateOnly? birthdate)
        {
            if (!birthdate.HasValue)
                return 0;

            var today = DateOnly.FromDateTime(DateTime.Today);
            
            // If birthdate is in the future, return 0
            if (birthdate.Value > today)
                return 0;

            var age = today.Year - birthdate.Value.Year;
            
            // If birthday hasn't occurred this year yet, subtract 1
            if (birthdate.Value > today.AddYears(-age))
                age--;

            // Ensure age is never negative
            return Math.Max(0, age);
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
            var birthdate = Request.Form["Birthdate"];
            var gender = Request.Form["Gender"];

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
                
                // Handle Birthdate
                if (!string.IsNullOrWhiteSpace(birthdate) && DateOnly.TryParse(birthdate, out DateOnly parsedBirthdate))
                {
                    existingStudent.Birthdate = parsedBirthdate;
                }
                else if (string.IsNullOrWhiteSpace(birthdate))
                {
                    // Allow clearing the birthdate if empty string is provided
                    existingStudent.Birthdate = null;
                }
                
                // Handle Gender
                if (!string.IsNullOrWhiteSpace(gender))
                {
                    existingStudent.Gender = gender.ToString().Trim();
                }
                else
                {
                    // Allow clearing the gender if empty string is provided
                    existingStudent.Gender = null;
                }

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
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error updating patient: {dbEx.Message}");
                
                // Check for specific database errors
                if (dbEx.InnerException != null && 
                    (dbEx.InnerException.Message.Contains("Duplicate entry") || 
                     dbEx.InnerException.Message.Contains("UNIQUE constraint")))
                {
                    if (dbEx.InnerException.Message.Contains("Email") || dbEx.InnerException.Message.Contains("email"))
                    {
                        return Json(new { success = false, message = "This email address is already in use. Please use a different email." });
                    }
                    if (dbEx.InnerException.Message.Contains("Idnumber") || dbEx.InnerException.Message.Contains("idnumber"))
                    {
                        return Json(new { success = false, message = "This ID number is already in use. Please use a different ID number." });
                    }
                }
                
                return Json(new { success = false, message = "An error occurred while updating patient information. Please try again." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating patient: {ex.Message}");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
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
                var wasInactive = !student.IsActive;
                
                // If trying to activate an account, check if email is verified first
                if (wasInactive && !student.IsEmailVerified)
                {
                    return Json(new { 
                        success = false, 
                        message = "Cannot activate account. Patient's email has not been verified yet. Please verify the email first before activating the account." 
                    });
                }
                
                // Toggle the IsActive status
                student.IsActive = !student.IsActive;
                await _context.SaveChangesAsync();

                // If activating an inactive account, send activation email
                if (wasInactive && student.IsActive && student.IsEmailVerified)
                {
                    var baseUrl = GetBaseUrl();
                    var loginUrl = $"{baseUrl}{Url.Action("Login", "Student")}";

                    Console.WriteLine($"[ACTIVATION] Attempting to send activation email to {student.Email}");
                    Console.WriteLine($"[ACTIVATION] Login URL: {loginUrl}");
                    
                    // Send email - await it but don't fail activation if email fails
                    try
                    {
                        await _emailService.SendAccountActivationEmail(student.Email, student.FirstName, loginUrl);
                        Console.WriteLine($"[ACTIVATION] Activation email sent successfully to {student.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log email error but don't fail activation
                        Console.WriteLine($"[ACTIVATION ERROR] Failed to send activation email to {student.Email}: {emailEx.Message}");
                        Console.WriteLine($"[ACTIVATION ERROR] Stack trace: {emailEx.StackTrace}");
                        if (emailEx.InnerException != null)
                        {
                            Console.WriteLine($"[ACTIVATION ERROR] Inner exception: {emailEx.InnerException.Message}");
                        }
                    }
                }

                string action = student.IsActive ? "activated" : "deactivated";
                string message = student.IsActive 
                    ? $"Patient {action} successfully. Activation email sent to {student.Email}."
                    : $"Patient {action} successfully.";
                    
                return Json(new { success = true, message = message, isActive = student.IsActive });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error updating patient status: {dbEx.Message}");
                return Json(new { success = false, message = "An error occurred while updating patient status. Please try again." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating patient status: {ex.Message}");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

    }
}