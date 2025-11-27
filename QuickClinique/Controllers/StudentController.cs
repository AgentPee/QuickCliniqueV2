using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Attributes;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Linq;

namespace QuickClinique.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly IFileStorageService _fileStorageService;

        public StudentController(ApplicationDbContext context, IEmailService emailService, IPasswordService passwordService, IFileStorageService fileStorageService)
        {
            _context = context;
            _emailService = emailService;
            _passwordService = passwordService;
            _fileStorageService = fileStorageService;
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

        // GET: Student
        public async Task<IActionResult> Index()
        {
            var students = _context.Students
                .Include(s => s.User);
            var result = await students.ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = result });

            return View(result);
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Student not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = student });

            return View(student);
        }

        // GET: Student/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role");

            if (IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    userTypes = ViewData["UserId"]
                });

            return View();
        }

        // POST: Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Idnumber,FirstName,LastName,Email,Password,PhoneNumber")] Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Student created successfully", id = student.StudentId });

                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", student.UserId);

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

            return View(student);
        }

        // GET: Student/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Student not found" });
                return NotFound();
            }

            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", student.UserId);

            if (IsAjaxRequest())
                return Json(new { success = true, data = student });

            return View(student);
        }

        // POST: Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,UserId,Idnumber,FirstName,LastName,Email,Password,PhoneNumber")] Student student)
        {
            if (id != student.StudentId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Student updated successfully" });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Student not found" });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", student.UserId);

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

            return View(student);
        }

        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Student not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = student });

            return View(student);
        }

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                .Include(s => s.Appointments)
                .Include(s => s.Histories)
                .Include(s => s.Notifications)
                .Include(s => s.Precords)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student != null)
            {
                try
                {
                    // Delete all related emergency records first
                    var emergencies = await _context.Emergencies
                        .Where(e => e.StudentId == id)
                        .ToListAsync();
                    
                    if (emergencies.Any())
                    {
                        _context.Emergencies.RemoveRange(emergencies);
                    }

                    // Delete related appointments
                    if (student.Appointments.Any())
                    {
                        _context.Appointments.RemoveRange(student.Appointments);
                    }

                    // Delete related histories
                    if (student.Histories.Any())
                    {
                        _context.Histories.RemoveRange(student.Histories);
                    }

                    // Delete related notifications
                    if (student.Notifications.Any())
                    {
                        _context.Notifications.RemoveRange(student.Notifications);
                    }

                    // Delete related precords (medical records)
                    if (student.Precords.Any())
                    {
                        _context.Precords.RemoveRange(student.Precords);
                    }

                    // Delete insurance receipt file if exists
                    if (!string.IsNullOrEmpty(student.InsuranceReceipt))
                    {
                        try
                        {
                            await _fileStorageService.DeleteFileAsync(student.InsuranceReceipt);
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail deletion if file deletion fails
                            Console.WriteLine($"Warning: Could not delete insurance receipt file: {ex.Message}");
                        }
                    }

                    // Delete student image file if exists
                    if (!string.IsNullOrEmpty(student.Image))
                    {
                        try
                        {
                            await _fileStorageService.DeleteFileAsync(student.Image);
                        }
                        catch (Exception ex)
                        {
                            // Log but don't fail deletion if file deletion fails
                            Console.WriteLine($"Warning: Could not delete student image file: {ex.Message}");
                        }
                    }

                    // Finally, delete the student
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Student deleted successfully" });
                }
                catch (DbUpdateException ex)
                {
                    Console.WriteLine($"Error deleting student: {ex.Message}");
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Failed to delete student. There may be related records that prevent deletion." });
                    
                    ModelState.AddModelError("", "Failed to delete student. There may be related records that prevent deletion.");
                    return View(student);
                }
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Student not found" });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }

        // GET: Student/Register
        public IActionResult Register()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Student/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StudentRegisterViewModel model)
        {
            // Validate required fields with specific error messages
            if (model.Idnumber == 0 || model.Idnumber < 10000000)
            {
                ModelState.AddModelError("Idnumber", "ID Number is required and must be at least 8 digits. Please enter your ID number.");
            }

            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                ModelState.AddModelError("FirstName", "First Name is required. Please enter your first name.");
            }

            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError("LastName", "Last Name is required. Please enter your last name.");
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email address is required. Please enter your email address.");
            }
            else if (!model.Email.Contains("@") || !model.Email.Contains("."))
            {
                ModelState.AddModelError("Email", "Please enter a valid email address.");
            }

            if (string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "Phone Number is required. Please enter your phone number.");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.PhoneNumber, @"^09[0-9]{9}$"))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number must start with 09 and be 11 digits total (e.g., 09345672824).");
            }

            if (string.IsNullOrWhiteSpace(model.Gender))
            {
                ModelState.AddModelError("Gender", "Gender is required. Please select your gender.");
            }

            if (model.Birthdate == default(DateOnly))
            {
                ModelState.AddModelError("Birthdate", "Birthdate is required. Please select your date of birth.");
            }

            if (model.StudentIdImageFront == null || model.StudentIdImageFront.Length == 0)
            {
                ModelState.AddModelError("StudentIdImageFront", "Student ID Front image is required. Please upload a photo of the front of your ID.");
            }

            if (model.StudentIdImageBack == null || model.StudentIdImageBack.Length == 0)
            {
                ModelState.AddModelError("StudentIdImageBack", "Student ID Back image is required. Please upload a photo of the back of your ID.");
            }

            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required. Please enter a password.");
            }
            else if (model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Password must be at least 6 characters long.");
            }

            if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Please confirm your password.");
            }
            else if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match. Please make sure both passwords are the same.");
            }

            // Validate Emergency Contact fields
            if (string.IsNullOrWhiteSpace(model.EmergencyContactName))
            {
                ModelState.AddModelError("EmergencyContactName", "Emergency Contact Name is required. Please enter the name of your emergency contact.");
            }

            if (string.IsNullOrWhiteSpace(model.EmergencyContactRelationship))
            {
                ModelState.AddModelError("EmergencyContactRelationship", "Emergency Contact Relationship is required. Please specify your relationship with the emergency contact.");
            }

            if (string.IsNullOrWhiteSpace(model.EmergencyContactPhoneNumber))
            {
                ModelState.AddModelError("EmergencyContactPhoneNumber", "Emergency Contact Phone Number is required. Please enter the phone number of your emergency contact.");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.EmergencyContactPhoneNumber, @"^09[0-9]{9}$"))
            {
                ModelState.AddModelError("EmergencyContactPhoneNumber", "Emergency contact phone number must start with 09 and be 11 digits total (e.g., 09345672824).");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if ID number already exists
                    if (await _context.Students.AnyAsync(x => x.Idnumber == model.Idnumber))
                    {
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "This ID number is already registered. Please use a different ID number or contact support if you believe this is an error.",
                                field = "Idnumber"
                            });

                        ModelState.AddModelError("Idnumber", "This ID number is already registered. Please use a different ID number or contact support if you believe this is an error.");
                        return View(model);
                    }

                    // Check if email already exists
                    if (await _context.Students.AnyAsync(x => x.Email.ToLower() == model.Email.ToLower()))
                    {
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "This email address is already registered. Please use a different email or try logging in instead.",
                                field = "Email"
                            });

                        ModelState.AddModelError("Email", "This email address is already registered. Please use a different email or try logging in instead.");
                        return View(model);
                    }

                    // Create and save the Usertype first
                    var usertype = new Usertype
                    {
                        Name = model.FirstName + " " + model.LastName,
                        Role = "Student"
                    };

                    try
                    {
                        _context.Usertypes.Add(usertype);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        Console.WriteLine($"Database error creating user type: {dbEx.Message}");
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "An error occurred while creating your account. Please try again. If the problem persists, contact support."
                            });

                        ModelState.AddModelError("", "An error occurred while creating your account. Please try again.");
                        return View(model);
                    }

                    // Generate email verification token
                    var emailToken = GenerateToken();

                    // Handle image uploads (Front and Back)
                    var imagePaths = new List<string>();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    const long maxFileSize = 5 * 1024 * 1024; // 5MB

                    // Process Front Image
                    if (model.StudentIdImageFront != null && model.StudentIdImageFront.Length > 0)
                    {
                        try
                        {
                            var fileExtension = Path.GetExtension(model.StudentIdImageFront.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "Invalid file type for ID front image. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).",
                                        field = "StudentIdImageFront"
                                    });

                                ModelState.AddModelError("StudentIdImageFront", "Invalid file type. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).");
                                return View(model);
                            }

                            if (model.StudentIdImageFront.Length > maxFileSize)
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "ID front image file size is too large. Maximum file size is 5MB. Please compress or resize your image.",
                                        field = "StudentIdImageFront"
                                    });

                                ModelState.AddModelError("StudentIdImageFront", "File size exceeds the maximum limit of 5MB. Please compress or resize your image.");
                                return View(model);
                            }

                            var frontFileName = $"{model.Idnumber}_front_{Guid.NewGuid()}";
                            var frontImagePath = await _fileStorageService.UploadFileAsync(model.StudentIdImageFront, "student-ids", frontFileName);
                            imagePaths.Add(frontImagePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error uploading front image: {ex.Message}");
                            if (IsAjaxRequest())
                                return Json(new { 
                                    success = false, 
                                    error = "Error saving ID front image. Please try again or contact support.",
                                    field = "StudentIdImageFront"
                                });

                            ModelState.AddModelError("StudentIdImageFront", "Error saving file. Please try again.");
                            return View(model);
                        }
                    }

                    // Process Back Image
                    if (model.StudentIdImageBack != null && model.StudentIdImageBack.Length > 0)
                    {
                        try
                        {
                            var fileExtension = Path.GetExtension(model.StudentIdImageBack.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "Invalid file type for ID back image. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).",
                                        field = "StudentIdImageBack"
                                    });

                                ModelState.AddModelError("StudentIdImageBack", "Invalid file type. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).");
                                return View(model);
                            }

                            if (model.StudentIdImageBack.Length > maxFileSize)
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "ID back image file size is too large. Maximum file size is 5MB. Please compress or resize your image.",
                                        field = "StudentIdImageBack"
                                    });

                                ModelState.AddModelError("StudentIdImageBack", "File size exceeds the maximum limit of 5MB. Please compress or resize your image.");
                                return View(model);
                            }

                            var backFileName = $"{model.Idnumber}_back_{Guid.NewGuid()}";
                            var backImagePath = await _fileStorageService.UploadFileAsync(model.StudentIdImageBack, "student-ids", backFileName);
                            imagePaths.Add(backImagePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error uploading back image: {ex.Message}");
                            if (IsAjaxRequest())
                                return Json(new { 
                                    success = false, 
                                    error = "Error saving ID back image. Please try again or contact support.",
                                    field = "StudentIdImageBack"
                                });

                            ModelState.AddModelError("StudentIdImageBack", "Error saving file. Please try again.");
                            return View(model);
                        }
                    }

                    // Combine image paths (comma-separated) or use first image if only one provided
                    string imagePath = imagePaths.Count > 0 ? string.Join(",", imagePaths) : null;

                    // Create the Student
                    var student = new Student
                    {
                        UserId = usertype.UserId,
                        Idnumber = model.Idnumber,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Gender = model.Gender,
                        Birthdate = model.Birthdate,
                        Image = imagePath,
                        Password = _passwordService.HashPassword(model.Password), // Hash the password
                        IsEmailVerified = false,
                        EmailVerificationToken = emailToken,
                        EmailVerificationTokenExpiry = DateTime.Now.AddHours(24),
                        IsActive = false, // New accounts are deactivated by default
                        EmergencyContactName = model.EmergencyContactName,
                        EmergencyContactRelationship = model.EmergencyContactRelationship,
                        EmergencyContactPhoneNumber = model.EmergencyContactPhoneNumber
                    };

                    try
                    {
                        _context.Students.Add(student);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        // Check for unique constraint violations
                        if (dbEx.InnerException != null && 
                            (dbEx.InnerException.Message.Contains("Duplicate entry") || 
                             dbEx.InnerException.Message.Contains("UNIQUE constraint") ||
                             dbEx.InnerException.Message.Contains("duplicate key")))
                        {
                            // Check which field caused the violation
                            if (dbEx.InnerException.Message.Contains("Idnumber") || dbEx.InnerException.Message.Contains("idnumber"))
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "This ID number is already registered. Please use a different ID number.",
                                        field = "Idnumber"
                                    });

                                ModelState.AddModelError("Idnumber", "This ID number is already registered. Please use a different ID number.");
                                return View(model);
                            }
                            else if (dbEx.InnerException.Message.Contains("Email") || dbEx.InnerException.Message.Contains("email"))
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "This email address is already registered. Please use a different email.",
                                        field = "Email"
                                    });

                                ModelState.AddModelError("Email", "This email address is already registered. Please use a different email.");
                                return View(model);
                            }
                        }

                        Console.WriteLine($"Database error saving student: {dbEx.Message}");
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "An error occurred while saving your registration. Please try again. If the problem persists, contact support."
                            });

                        ModelState.AddModelError("", "An error occurred while saving your registration. Please try again.");
                        return View(model);
                    }

                    // Send verification email (fire-and-forget)
                    try
                    {
                        var baseUrl = GetBaseUrl();
                        var verificationLink = $"{baseUrl}{Url.Action("VerifyEmail", "Student", new { token = emailToken, email = student.Email })}";

                        // Fire-and-forget: don't await, let it run in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendVerificationEmail(student.Email, student.FirstName, verificationLink);
                                Console.WriteLine($"[EMAIL] Verification email sent successfully to {student.Email}");
                            }
                            catch (Exception emailEx)
                            {
                                Console.WriteLine($"[EMAIL ERROR] Failed to send verification email to {student.Email}: {emailEx.Message}");
                            }
                        });
                    }
                    catch (Exception emailEx)
                    {
                        // Log email error but don't fail registration
                        Console.WriteLine($"Error sending verification email: {emailEx.Message}");
                        // Continue with registration success - user can request resend later
                    }

                    if (IsAjaxRequest())
                        return Json(new { 
                            success = true, 
                            message = "Registration successful! Please check your email to verify your account. Your account will be activated by an administrator after verification. You will receive an email once your account is activated.",
                            redirectUrl = Url.Action("verificationE", "Student", new { email = student.Email }),
                            studentEmail = student.Email,
                            studentIdNumber = student.Idnumber
                        });

                    TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account. Your account will be activated by an administrator after verification. You will receive an email once your account is activated.";
                    TempData["StudentEmail"] = student.Email;
                    TempData["StudentIdNumber"] = student.Idnumber;
                    return RedirectToAction("verificationE", "Student", new { email = student.Email });
                }
                catch (DbUpdateException dbEx)
                {
                    Console.WriteLine($"Database error during registration: {dbEx.Message}");
                    if (IsAjaxRequest())
                        return Json(new { 
                            success = false, 
                            error = "A database error occurred. Please try again. If the problem persists, contact support."
                        });

                    ModelState.AddModelError("", "A database error occurred. Please try again.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error during registration: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (IsAjaxRequest())
                        return Json(new { 
                            success = false, 
                            error = "An unexpected error occurred during registration. Please try again. If the problem persists, contact support."
                        });

                    ModelState.AddModelError("", "An unexpected error occurred during registration. Please try again.");
                }
            }

            if (IsAjaxRequest())
            {
                // Get the first error message for the main error field
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault();

                // Build detailed error response
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

                // Count missing required fields
                var missingFields = errors.Keys.Where(k => 
                    errors[k].Any(e => e.Contains("required") || e.Contains("Required"))).ToList();

                string mainError;
                if (missingFields.Count > 0)
                {
                    if (missingFields.Count == 1)
                    {
                        mainError = $"Please fill in the required field: {string.Join(", ", missingFields)}";
                    }
                    else
                    {
                        mainError = $"Please fill in all required fields: {string.Join(", ", missingFields)}";
                    }
                }
                else
                {
                    mainError = firstError ?? "Please correct the errors below and try again.";
                }

                return Json(new
                {
                    success = false,
                    error = mainError,
                    errors = errors,
                    missingFields = missingFields
                });
            }

            return View(model);
        }

        // GET: Student/Login
        public IActionResult Login()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Student/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(StudentLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Idnumber == model.Idnumber);

                if (student != null && !_passwordService.VerifyPassword(model.Password, student.Password))
                {
                    student = null; // Invalid password
                }

                if (student != null)
                {
                    if (!student.IsEmailVerified)
                    {
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "Please verify your email before logging in.",
                                requiresVerification = true,
                                redirectUrl = Url.Action("verificationE", "Student", new { email = student.Email }),
                                email = student.Email,
                                idNumber = student.Idnumber
                            });

                        TempData["ErrorMessage"] = "Please verify your email before logging in.";
                        return RedirectToAction("verificationE", "Student", new { email = student.Email });
                    }

                    if (!student.IsActive)
                    {
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "Your account is pending activation. Please wait for clinic staff to activate your account. You will receive an email notification once your account is activated. If you have any questions, please contact the clinic staff.",
                                pendingActivation = true
                            });

                        ModelState.AddModelError("", "Your account is pending activation. Please wait for clinic staff to activate your account. You will receive an email notification once your account is activated. If you have any questions, please contact the clinic staff.");
                        return View(model);
                    }

                    // Set session or authentication cookie
                    HttpContext.Session.SetInt32("StudentId", student.StudentId);
                    HttpContext.Session.SetString("StudentName", student.FirstName + " " + student.LastName);

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Login successful!", redirectUrl = Url.Action("Dashboard", "Student") });

                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Dashboard", "Student");
                }

                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Invalid ID number or password." });

                ModelState.AddModelError("", "Invalid ID number or password.");
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

            return View(model);
        }

        // GET: Student/Dashboard
        [StudentOnly]
        public async Task<IActionResult> Dashboard()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (!studentId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return RedirectToAction(nameof(Login));
            }

            return View(student);
        }

        // GET: Student/Profile
        [StudentOnly]
        public async Task<IActionResult> Profile()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (!studentId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return RedirectToAction(nameof(Login));
            }

            return View(student);
        }

        // POST: Student/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [StudentOnly]
        public async Task<IActionResult> UpdateProfile(Student model, IFormFile? InsuranceReceiptFile)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (!studentId.HasValue || studentId.Value != model.StudentId)
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            var student = await _context.Students.FindAsync(model.StudentId);
            if (student == null)
            {
                return Json(new { success = false, error = "Student not found" });
            }

            // Handle insurance receipt file upload
            if (InsuranceReceiptFile != null && InsuranceReceiptFile.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                var fileExtension = Path.GetExtension(InsuranceReceiptFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Invalid file type. Please upload an image file (jpg, jpeg, png, gif, bmp, or webp)." });

                    ModelState.AddModelError("InsuranceReceiptFile", "Invalid file type. Please upload an image file.");
                    return View(model);
                }

                // Validate file size (max 5MB)
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                if (InsuranceReceiptFile.Length > maxFileSize)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "File size exceeds the maximum limit of 5MB." });

                    ModelState.AddModelError("InsuranceReceiptFile", "File size exceeds the maximum limit of 5MB.");
                    return View(model);
                }

                // Delete old file if exists
                if (!string.IsNullOrEmpty(student.InsuranceReceipt))
                {
                    await _fileStorageService.DeleteFileAsync(student.InsuranceReceipt);
                }

                // Generate unique filename
                var fileName = $"{student.Idnumber}_{Guid.NewGuid()}";
                
                // Upload the file using storage service
                student.InsuranceReceipt = await _fileStorageService.UploadFileAsync(InsuranceReceiptFile, "insurance-receipts", fileName);
            }

            // Validate emergency contact phone number format if provided
            if (!string.IsNullOrWhiteSpace(model.EmergencyContactPhoneNumber))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.EmergencyContactPhoneNumber, @"^09[0-9]{9}$"))
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Emergency contact phone number must start with 09 and be 11 digits total (e.g., 09345672824)." });
                    
                    ModelState.AddModelError("EmergencyContactPhoneNumber", "Emergency contact phone number must start with 09 and be 11 digits total.");
                    return View(model);
                }
            }

            // Update only allowed fields
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.Email = model.Email;
            student.PhoneNumber = model.PhoneNumber;
            student.Birthdate = model.Birthdate;
            student.Gender = model.Gender;
            
            // Update emergency contact information
            student.EmergencyContactName = model.EmergencyContactName;
            student.EmergencyContactRelationship = model.EmergencyContactRelationship;
            student.EmergencyContactPhoneNumber = model.EmergencyContactPhoneNumber;

            await _context.SaveChangesAsync();

            // Update session name
            HttpContext.Session.SetString("StudentName", student.FirstName + " " + student.LastName);

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Profile updated successfully" });

            TempData["SuccessMessage"] = "Profile updated successfully";
            return RedirectToAction(nameof(Profile));
        }

        // POST: Student/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [StudentOnly]
        public async Task<IActionResult> ChangePassword(int studentId, string currentPassword, string newPassword, string confirmPassword)
        {
            var sessionStudentId = HttpContext.Session.GetInt32("StudentId");
            if (!sessionStudentId.HasValue || sessionStudentId.Value != studentId)
            {
                return Json(new { success = false, error = "Unauthorized" });
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return Json(new { success = false, error = "Current password is required", field = "currentPassword" });
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return Json(new { success = false, error = "New password is required", field = "newPassword" });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, error = "Passwords do not match", field = "confirmPassword" });
            }

            // Validate password strength
            if (newPassword.Length < 6)
            {
                return Json(new { success = false, error = "Password must be at least 6 characters", field = "newPassword" });
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null)
            {
                return Json(new { success = false, error = "Student not found" });
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(currentPassword, student.Password))
            {
                return Json(new { success = false, error = "Current password is incorrect", field = "currentPassword" });
            }

            // Check if new password is same as current
            if (_passwordService.VerifyPassword(newPassword, student.Password))
            {
                return Json(new { success = false, error = "New password must be different from current password", field = "newPassword" });
            }

            // Update password
            student.Password = _passwordService.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully" });
        }

        // GET: Student/History
        [StudentOnly]
        public async Task<IActionResult> History()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (!studentId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            var appointments = await _context.Appointments
                .Include(a => a.Schedule)
                .Where(a => a.PatientId == studentId.Value)
                .OrderByDescending(a => a.DateBooked)
                .ToListAsync();

            var emergencies = await _context.Emergencies
                .Where(e => e.StudentId == studentId.Value)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            ViewBag.Emergencies = emergencies;

            return View(appointments);
        }

        // GET: Student/EHR - Electronic Health Records
        [StudentOnly]
        public async Task<IActionResult> EHR()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (!studentId.HasValue)
            {
                return RedirectToAction(nameof(Login));
            }

            var student = await _context.Students
                .Include(s => s.Precords)
                .Include(s => s.Appointments)
                    .ThenInclude(a => a.Schedule)
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Order appointments by date descending (most recent first)
            var orderedAppointments = student.Appointments
                .OrderByDescending(a => a.Schedule != null ? a.Schedule.Date : DateOnly.MinValue)
                .ThenByDescending(a => a.Schedule != null ? a.Schedule.StartTime : TimeOnly.MinValue)
                .ToList();

            student.Appointments = orderedAppointments;

            return View(student);
        }

        // GET: Student/GetCurrentStudentId - Get logged in student ID
        [HttpGet]
        public IActionResult GetCurrentStudentId()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            
            if (studentId.HasValue)
            {
                return Json(new { success = true, studentId = studentId.Value });
            }
            
            return Json(new { success = false, error = "No student is currently logged in" });
        }

        // GET: Student/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            if (IsAjaxRequest())
                return Json(new { success = true, message = "You have been logged out successfully.", redirectUrl = Url.Action(nameof(Login)) });

            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Student/verificationE - Display email verification page
        public IActionResult verificationE(string? email = null)
        {
            // Pass email to view if provided
            if (!string.IsNullOrEmpty(email))
            {
                ViewBag.Email = email;
            }
            else if (TempData["StudentEmail"] != null)
            {
                ViewBag.Email = TempData["StudentEmail"].ToString();
                TempData.Keep("StudentEmail");
            }

            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // GET: Student/VerifyEmail
        public async Task<IActionResult> VerifyEmail(string token, string email)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email &&
                         s.EmailVerificationToken == token &&
                         s.EmailVerificationTokenExpiry > DateTime.Now);

            if (student == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Invalid or expired verification link." });

                TempData["ErrorMessage"] = "Invalid or expired verification link.";
                return RedirectToAction(nameof(Login));
            }

            student.IsEmailVerified = true;
            student.EmailVerificationToken = null;
            student.EmailVerificationTokenExpiry = null;

            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Email verified successfully! You can now login.", redirectUrl = Url.Action(nameof(Login)) });

            TempData["SuccessMessage"] = "Email verified successfully! You can now login.";
            return RedirectToAction(nameof(Login));
        }

        // POST: Student/ResendVerificationEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail()
        {
            try
            {
                string? email = null;
                int? idNumber = null;

                // Get from form data (FormData submissions)
                email = Request.Form["Email"].FirstOrDefault();
                var idNumberStr = Request.Form["IdNumber"].FirstOrDefault();
                if (!string.IsNullOrEmpty(idNumberStr) && int.TryParse(idNumberStr, out int parsedId))
                {
                    idNumber = parsedId;
                }

                if (string.IsNullOrEmpty(email) && !idNumber.HasValue)
                {
                    return Json(new { success = false, error = "Email or ID Number is required." });
                }

                Student? student = null;

                if (!string.IsNullOrEmpty(email))
                {
                    student = await _context.Students
                        .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
                }
                else if (idNumber.HasValue)
                {
                    student = await _context.Students
                        .FirstOrDefaultAsync(s => s.Idnumber == idNumber.Value);
                }

                if (student == null)
                {
                    // Don't reveal that the user doesn't exist (security best practice)
                    return Json(new { 
                        success = true, 
                        message = "If your email is registered, a verification email will be sent." 
                    });
                }

                if (student.IsEmailVerified)
                {
                    return Json(new { 
                        success = false, 
                        error = "Your email is already verified. You can login now." 
                    });
                }

                // Generate new token and extend expiry
                var newToken = GenerateToken();
                student.EmailVerificationToken = newToken;
                student.EmailVerificationTokenExpiry = DateTime.Now.AddHours(24);

                await _context.SaveChangesAsync();

                // Send verification email (fire-and-forget)
                var baseUrl = GetBaseUrl();
                var verificationLink = $"{baseUrl}{Url.Action("VerifyEmail", "Student", new { token = newToken, email = student.Email })}";

                // Fire-and-forget: don't await, let it run in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendVerificationEmail(student.Email, student.FirstName, verificationLink);
                        Console.WriteLine($"[EMAIL] Verification email sent successfully to {student.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"[EMAIL ERROR] Failed to send verification email to {student.Email}: {emailEx.Message}");
                    }
                });

                return Json(new { 
                    success = true, 
                    message = "Verification email has been sent. Please check your inbox (and spam folder)." 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resending verification email: {ex.Message}");
                return Json(new { 
                    success = false, 
                    error = "An error occurred while sending the verification email. Please try again later." 
                });
            }
        }

        // Helper class for resend verification request
        public class ResendVerificationRequest
        {
            public string? Email { get; set; }
            public int? IdNumber { get; set; }
        }

        // GET: Student/resetPasswordE - Display password reset email page
        public IActionResult resetPasswordE(string? email = null)
        {
            // Pass email to view if provided
            if (!string.IsNullOrEmpty(email))
            {
                ViewBag.Email = email;
            }
            else if (TempData["StudentEmail"] != null)
            {
                ViewBag.Email = TempData["StudentEmail"].ToString();
                TempData.Keep("StudentEmail");
            }

            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Student/ResendPasswordResetEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendPasswordResetEmail()
        {
            try
            {
                string? email = null;

                // Get from form data (FormData submissions)
                email = Request.Form["Email"].FirstOrDefault();

                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, error = "Email is required." });
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower() && s.IsEmailVerified);

                if (student == null)
                {
                    // Don't reveal that the user doesn't exist (security best practice)
                    return Json(new { 
                        success = true, 
                        message = "If your email is registered and verified, a password reset email will be sent." 
                    });
                }

                // Generate new token and extend expiry
                var resetToken = GenerateToken();
                student.PasswordResetToken = resetToken;
                student.PasswordResetTokenExpiry = DateTime.Now.AddHours(1);

                await _context.SaveChangesAsync();

                // Send password reset email (fire-and-forget)
                var baseUrl = GetBaseUrl();
                var resetLink = $"{baseUrl}{Url.Action("ResetPassword", "Student", new { token = resetToken, email = student.Email })}";

                // Fire-and-forget: don't await, let it run in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendPasswordResetEmail(student.Email, student.FirstName, resetLink);
                        Console.WriteLine($"[EMAIL] Password reset email sent successfully to {student.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"[EMAIL ERROR] Failed to send password reset email to {student.Email}: {emailEx.Message}");
                    }
                });

                return Json(new { 
                    success = true, 
                    message = "Password reset email has been sent. Please check your inbox (and spam folder)." 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resending password reset email: {ex.Message}");
                return Json(new { 
                    success = false, 
                    error = "An error occurred while sending the password reset email. Please try again later." 
                });
            }
        }

        // Helper class for resend password reset request
        public class ResendPasswordResetRequest
        {
            public string? Email { get; set; }
        }

        // GET: Student/ForgotPassword
        public IActionResult ForgotPassword()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Student/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email == model.Email && s.IsEmailVerified);

                if (student != null)
                {
                    var resetToken = GenerateToken();
                    student.PasswordResetToken = resetToken;
                    student.PasswordResetTokenExpiry = DateTime.Now.AddHours(1);

                    await _context.SaveChangesAsync();

                    var baseUrl = GetBaseUrl();
                    var resetLink = $"{baseUrl}{Url.Action("ResetPassword", "Student", new { token = resetToken, email = student.Email })}";

                    await _emailService.SendPasswordResetEmail(student.Email, student.FirstName, resetLink);

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Password reset link has been sent to your email.", redirectUrl = Url.Action("resetPasswordE", "Student", new { email = student.Email }) });

                    TempData["SuccessMessage"] = "Password reset link has been sent to your email.";
                    return RedirectToAction("resetPasswordE", "Student", new { email = student.Email });
                }

                // Don't reveal that the user doesn't exist or isn't verified
                if (IsAjaxRequest())
                    return Json(new { success = true, message = "If your email is registered and verified, you will receive a password reset link.", redirectUrl = Url.Action("resetPasswordE", "Student", new { email = model.Email }) });

                TempData["SuccessMessage"] = "If your email is registered and verified, you will receive a password reset link.";
                return RedirectToAction("resetPasswordE", "Student", new { email = model.Email });
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

            return View(model);
        }

        // GET: Student/ResetPassword
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email &&
                         s.PasswordResetToken == token &&
                         s.PasswordResetTokenExpiry > DateTime.Now);

            if (student == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Invalid or expired reset link." });

                TempData["ErrorMessage"] = "Invalid or expired reset link.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            if (IsAjaxRequest())
                return Json(new { success = true, data = model });

            return View(model);
        }

        // POST: Student/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email == model.Email &&
                             s.PasswordResetToken == model.Token &&
                             s.PasswordResetTokenExpiry > DateTime.Now);

                if (student == null)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Invalid or expired reset link." });

                    TempData["ErrorMessage"] = "Invalid or expired reset link.";
                    return RedirectToAction(nameof(ForgotPassword));
                }

                // Update password
                student.Password = _passwordService.HashPassword(model.Password);
                student.PasswordResetToken = null;
                student.PasswordResetTokenExpiry = null;

                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Password reset successfully! You can now login with your new password.", redirectUrl = Url.Action(nameof(Login)) });

                TempData["SuccessMessage"] = "Password reset successfully! You can now login with your new password.";
                return RedirectToAction(nameof(Login));
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

            return View(model);
        }

        // POST: Student/SendSOS - Send emergency SOS request
        [HttpPost]
        [StudentOnly]
        public async Task<IActionResult> SendSOS([FromBody] SendSOSRequest request)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return Json(new { success = false, error = "Not logged in" });
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);

            if (student == null)
            {
                return Json(new { success = false, error = "Student not found" });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return Json(new { success = false, error = "Location is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Needs))
            {
                return Json(new { success = false, error = "At least one need must be specified" });
            }

            // Create emergency record
            var emergency = new Emergency
            {
                StudentId = student.StudentId,
                StudentName = $"{student.FirstName} {student.LastName}",
                StudentIdNumber = student.Idnumber,
                Location = request.Location,
                Needs = request.Needs,
                IsResolved = false,
                CreatedAt = DateTime.Now
            };

            _context.Emergencies.Add(emergency);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "SOS emergency request sent successfully",
                emergencyId = emergency.EmergencyId
            });
        }

        // GET: Student/CheckEmergencyResolved - Check if student's emergency was resolved
        [HttpGet]
        [StudentOnly]
        public async Task<IActionResult> CheckEmergencyResolved()
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (studentId == null)
            {
                return Json(new { success = false, error = "Not logged in" });
            }

            // Get the most recent unresolved emergency for this student
            var emergency = await _context.Emergencies
                .Where(e => e.StudentId == studentId.Value && !e.IsResolved)
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefaultAsync();

            if (emergency == null)
            {
                // Check if there's a recently resolved emergency (within last 5 minutes)
                var recentlyResolved = await _context.Emergencies
                    .Where(e => e.StudentId == studentId.Value && e.IsResolved)
                    .OrderByDescending(e => e.CreatedAt)
                    .FirstOrDefaultAsync();

                if (recentlyResolved != null && recentlyResolved.CreatedAt.HasValue)
                {
                    var timeSinceCreated = DateTime.Now - recentlyResolved.CreatedAt.Value;
                    // If resolved within last 5 minutes, return it
                    if (timeSinceCreated.TotalMinutes <= 5)
                    {
                        return Json(new { 
                            success = true, 
                            resolved = true,
                            emergencyId = recentlyResolved.EmergencyId,
                            message = "Your SOS alert has been received. Help is on the way."
                        });
                    }
                }

                return Json(new { success = true, resolved = false });
            }

            return Json(new { success = true, resolved = false, emergencyId = emergency.EmergencyId });
        }

        // Helper method to generate tokens
        private string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }

    public class SendSOSRequest
    {
        public string Location { get; set; } = string.Empty;
        public string Needs { get; set; } = string.Empty;
    }
}