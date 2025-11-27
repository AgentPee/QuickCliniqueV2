using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Attributes;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace QuickClinique.Controllers
{
    public class ClinicstaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly IFileStorageService _fileStorageService;

        public ClinicstaffController(ApplicationDbContext context, IEmailService emailService, IPasswordService passwordService, IFileStorageService fileStorageService)
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

        // GET: Clinicstaff
        public async Task<IActionResult> Index()
        {
            var clinicstaffs = await _context.Clinicstaffs
                .Include(c => c.User)
                .ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = clinicstaffs });

            return View(clinicstaffs);
        }

        // GET: Clinicstaff/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var clinicstaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClinicStaffId == id);

            if (clinicstaff == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Staff not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = clinicstaff });

            return View(clinicstaff);
        }

        // GET: Clinicstaff/Create
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

        // POST: Clinicstaff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FirstName,LastName,Email,PhoneNumber,Password")] Clinicstaff clinicstaff)
        {
            if (ModelState.IsValid)
            {
                _context.Add(clinicstaff);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Staff created successfully", id = clinicstaff.ClinicStaffId });

                return RedirectToAction(nameof(Index));
            }

            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", clinicstaff.UserId);

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

            return View(clinicstaff);
        }

        // GET: Clinicstaff/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var clinicstaff = await _context.Clinicstaffs.FindAsync(id);
            if (clinicstaff == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Staff not found" });
                return NotFound();
            }

            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", clinicstaff.UserId);

            if (IsAjaxRequest())
                return Json(new { success = true, data = clinicstaff });

            return View(clinicstaff);
        }

        // POST: Clinicstaff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClinicStaffId,UserId,FirstName,LastName,Email,PhoneNumber")] Clinicstaff clinicstaff, string? newPassword, string? confirmPassword)
        {
            Console.WriteLine($"=== EDIT POST STARTED ===");
            Console.WriteLine($"ID: {id}, ClinicStaffId: {clinicstaff.ClinicStaffId}");

            if (id != clinicstaff.ClinicStaffId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            // Remove validation for navigation properties and password
            ModelState.Remove("User");
            ModelState.Remove("Password");
            ModelState.Remove("Notifications");
            ModelState.Remove("newPassword");
            ModelState.Remove("confirmPassword");

            // Validate password confirmation if new password is provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Please confirm your password." });
                    ModelState.AddModelError("confirmPassword", "Please confirm your password.");
                }
                else if (newPassword != confirmPassword)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Passwords do not match." });
                    ModelState.AddModelError("confirmPassword", "Passwords do not match.");
                }
                else if (newPassword.Length < 6)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "Password must be at least 6 characters long." });
                    ModelState.AddModelError("newPassword", "Password must be at least 6 characters long.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(confirmPassword))
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Please enter a new password first." });
                ModelState.AddModelError("confirmPassword", "Please enter a new password first.");
            }

            Console.WriteLine($"ModelState IsValid after removals: {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("ModelState is valid, proceeding with update...");

                    // Find the existing entity in the context
                    var existingStaff = await _context.Clinicstaffs
                        .Include(c => c.User)
                        .FirstOrDefaultAsync(c => c.ClinicStaffId == id);

                    if (existingStaff == null)
                    {
                        Console.WriteLine("Existing staff not found in database");
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Staff not found" });
                        return NotFound();
                    }

                    Console.WriteLine($"Found existing staff: {existingStaff.FirstName} {existingStaff.LastName}");

                    // Update the Clinicstaff properties
                    existingStaff.FirstName = clinicstaff.FirstName;
                    existingStaff.LastName = clinicstaff.LastName;
                    existingStaff.Email = clinicstaff.Email;
                    existingStaff.PhoneNumber = clinicstaff.PhoneNumber;

                    // Only update password if a new one was provided
                    if (!string.IsNullOrWhiteSpace(newPassword))
                    {
                        Console.WriteLine("New password provided, updating password");
                        existingStaff.Password = _passwordService.HashPassword(newPassword);
                    }
                    else
                    {
                        Console.WriteLine("No new password provided, keeping existing password");
                    }

                    // Update the associated Usertype record
                    if (existingStaff.User != null)
                    {
                        var newFullName = $"{clinicstaff.FirstName} {clinicstaff.LastName}";
                        if (existingStaff.User.Name != newFullName)
                        {
                            Console.WriteLine($"Updating Usertype Name from '{existingStaff.User.Name}' to '{newFullName}'");
                            existingStaff.User.Name = newFullName;
                        }
                        else
                        {
                            Console.WriteLine("Usertype Name unchanged, no update needed");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No associated Usertype found for this staff member");
                    }

                    // Save changes
                    int changes = await _context.SaveChangesAsync();
                    Console.WriteLine($"SaveChanges completed. {changes} records affected.");

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Staff member updated successfully!" });

                    TempData["SuccessMessage"] = "Staff member updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"DbUpdateConcurrencyException: {ex.Message}");
                    if (!ClinicstaffExists(clinicstaff.ClinicStaffId))
                    {
                        Console.WriteLine("Clinic staff no longer exists");
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Staff not found" });
                        return NotFound();
                    }
                    else
                    {
                        Console.WriteLine("Concurrency conflict occurred");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during save: {ex.Message}");
                    Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred while saving. Please try again." });

                    ModelState.AddModelError("", "An error occurred while saving. Please try again.");
                }
            }
            else
            {
                Console.WriteLine("ModelState is still invalid. Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    if (errors.Count > 0)
                    {
                        Console.WriteLine($"  {key}: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            // If we got this far, something failed; redisplay form
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", clinicstaff.UserId);

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

            return View(clinicstaff);
        }

        // GET: Clinicstaff/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var clinicstaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClinicStaffId == id);

            if (clinicstaff == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Staff not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = clinicstaff });

            return View(clinicstaff);
        }

        // POST: Clinicstaff/ToggleActiveStaff/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActiveStaff(int id)
        {
            var clinicstaff = await _context.Clinicstaffs
                .FirstOrDefaultAsync(c => c.ClinicStaffId == id);

            if (clinicstaff == null)
            {
                return Json(new { success = false, message = "Staff member not found" });
            }

            try
            {
                // Toggle the IsActive status
                clinicstaff.IsActive = !clinicstaff.IsActive;
                await _context.SaveChangesAsync();

                string action = clinicstaff.IsActive ? "activated" : "deactivated";
                return Json(new { success = true, message = $"Staff member {action} successfully", isActive = clinicstaff.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating staff status: {ex.Message}" });
            }
        }

        private bool ClinicstaffExists(int id)
        {
            return _context.Clinicstaffs.Any(e => e.ClinicStaffId == id);
        }

        // GET: Clinicstaff/Login
        public IActionResult Login()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Clinicstaff/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(ClinicStaffLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = await _context.Clinicstaffs
                    .FirstOrDefaultAsync(s => s.Email == model.Email);

                if (staff != null && !_passwordService.VerifyPassword(model.Password, staff.Password))
                {
                    staff = null; // Invalid password
                }

                if (staff != null)
                {
                    if (!staff.IsEmailVerified)
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Please verify your email before logging in." });

                        ModelState.AddModelError("", "Please verify your email before logging in.");
                        return View(model);
                    }

                    if (!staff.IsActive)
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Your account has been deactivated. Please contact the administrator for assistance." });

                        ModelState.AddModelError("", "Your account has been deactivated. Please contact the administrator for assistance.");
                        return View(model);
                    }

                    // Set session
                    HttpContext.Session.SetInt32("ClinicStaffId", staff.ClinicStaffId);
                    HttpContext.Session.SetString("ClinicStaffName", staff.FirstName + " " + staff.LastName);
                    HttpContext.Session.SetString("UserRole", "ClinicStaff");

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Login successful!", redirectUrl = Url.Action("Index", "Dashboard") });

                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Index", "Dashboard");
                }

                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Invalid email or password." });

                ModelState.AddModelError("", "Invalid email or password.");
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

        // GET: Clinicstaff/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            if (IsAjaxRequest())
                return Json(new { success = true, message = "You have been logged out successfully.", redirectUrl = Url.Action(nameof(Login)) });

            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Clinicstaff/Register
        public IActionResult Register()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Clinicstaff/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(ClinicStaffRegisterViewModel model)
        {
            // Validate required fields with specific error messages
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

            if (model.StaffIdImageFront == null || model.StaffIdImageFront.Length == 0)
            {
                ModelState.AddModelError("StaffIdImageFront", "Staff ID Front image is required. Please upload a photo of the front of your ID.");
            }

            if (model.StaffIdImageBack == null || model.StaffIdImageBack.Length == 0)
            {
                ModelState.AddModelError("StaffIdImageBack", "Staff ID Back image is required. Please upload a photo of the back of your ID.");
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

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    if (await _context.Clinicstaffs.AnyAsync(x => x.Email.ToLower() == model.Email.ToLower()))
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
                        Role = "ClinicStaff"
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
                    if (model.StaffIdImageFront != null && model.StaffIdImageFront.Length > 0)
                    {
                        try
                        {
                            var fileExtension = Path.GetExtension(model.StaffIdImageFront.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "Invalid file type for ID front image. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).",
                                        field = "StaffIdImageFront"
                                    });

                                ModelState.AddModelError("StaffIdImageFront", "Invalid file type. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).");
                                return View(model);
                            }

                            if (model.StaffIdImageFront.Length > maxFileSize)
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "ID front image file size is too large. Maximum file size is 5MB. Please compress or resize your image.",
                                        field = "StaffIdImageFront"
                                    });

                                ModelState.AddModelError("StaffIdImageFront", "File size exceeds the maximum limit of 5MB. Please compress or resize your image.");
                                return View(model);
                            }

                            var frontFileName = $"staff_{Guid.NewGuid()}_front";
                            var frontImagePath = await _fileStorageService.UploadFileAsync(model.StaffIdImageFront, "staff-ids", frontFileName);
                            imagePaths.Add(frontImagePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error uploading front image: {ex.Message}");
                            if (IsAjaxRequest())
                                return Json(new { 
                                    success = false, 
                                    error = "Error saving ID front image. Please try again or contact support.",
                                    field = "StaffIdImageFront"
                                });

                            ModelState.AddModelError("StaffIdImageFront", "Error saving file. Please try again.");
                            return View(model);
                        }
                    }

                    // Process Back Image
                    if (model.StaffIdImageBack != null && model.StaffIdImageBack.Length > 0)
                    {
                        try
                        {
                            var fileExtension = Path.GetExtension(model.StaffIdImageBack.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "Invalid file type for ID back image. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).",
                                        field = "StaffIdImageBack"
                                    });

                                ModelState.AddModelError("StaffIdImageBack", "Invalid file type. Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP, or WEBP).");
                                return View(model);
                            }

                            if (model.StaffIdImageBack.Length > maxFileSize)
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "ID back image file size is too large. Maximum file size is 5MB. Please compress or resize your image.",
                                        field = "StaffIdImageBack"
                                    });

                                ModelState.AddModelError("StaffIdImageBack", "File size exceeds the maximum limit of 5MB. Please compress or resize your image.");
                                return View(model);
                            }

                            var backFileName = $"staff_{Guid.NewGuid()}_back";
                            var backImagePath = await _fileStorageService.UploadFileAsync(model.StaffIdImageBack, "staff-ids", backFileName);
                            imagePaths.Add(backImagePath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error uploading back image: {ex.Message}");
                            if (IsAjaxRequest())
                                return Json(new { 
                                    success = false, 
                                    error = "Error saving ID back image. Please try again or contact support.",
                                    field = "StaffIdImageBack"
                                });

                            ModelState.AddModelError("StaffIdImageBack", "Error saving file. Please try again.");
                            return View(model);
                        }
                    }

                    // Combine image paths (comma-separated) or use first image if only one provided
                    string imagePath = imagePaths.Count > 0 ? string.Join(",", imagePaths) : null;

                    // Create the Clinicstaff
                    var clinicstaff = new Clinicstaff
                    {
                        UserId = usertype.UserId,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Gender = model.Gender,
                        Birthdate = model.Birthdate,
                        Image = imagePath,
                        Password = _passwordService.HashPassword(model.Password),
                        IsEmailVerified = false,
                        EmailVerificationToken = emailToken,
                        EmailVerificationTokenExpiry = DateTime.Now.AddHours(24)
                    };

                    try
                    {
                        _context.Clinicstaffs.Add(clinicstaff);
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
                            if (dbEx.InnerException.Message.Contains("Email") || dbEx.InnerException.Message.Contains("email"))
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
                            else if (dbEx.InnerException.Message.Contains("PhoneNumber") || dbEx.InnerException.Message.Contains("phone"))
                            {
                                if (IsAjaxRequest())
                                    return Json(new { 
                                        success = false, 
                                        error = "This phone number is already registered. Please use a different phone number.",
                                        field = "PhoneNumber"
                                    });

                                ModelState.AddModelError("PhoneNumber", "This phone number is already registered. Please use a different phone number.");
                                return View(model);
                            }
                        }

                        Console.WriteLine($"Database error saving clinic staff: {dbEx.Message}");
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "An error occurred while saving your registration. Please try again. If the problem persists, contact support."
                            });

                        ModelState.AddModelError("", "An error occurred while saving your registration. Please try again.");
                        return View(model);
                    }

                    // Send verification email
                    try
                    {
                        var baseUrl = GetBaseUrl();
                        var verificationLink = $"{baseUrl}{Url.Action("VerifyEmail", "Clinicstaff", new { token = emailToken, email = clinicstaff.Email })}";

                        await _emailService.SendVerificationEmail(clinicstaff.Email, clinicstaff.FirstName, verificationLink);
                    }
                    catch (Exception emailEx)
                    {
                        // Log email error but don't fail registration
                        Console.WriteLine($"Error sending verification email: {emailEx.Message}");
                        // Continue with registration success - user can request resend later
                    }

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Registration successful! Please check your email to verify your account.", redirectUrl = Url.Action(nameof(Login)) });

                    TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
                    return RedirectToAction(nameof(Login));
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

        // GET: Clinicstaff/VerifyEmail
        public async Task<IActionResult> VerifyEmail(string token, string email)
        {
            var clinicstaff = await _context.Clinicstaffs
                .FirstOrDefaultAsync(s => s.Email == email &&
                         s.EmailVerificationToken == token &&
                         s.EmailVerificationTokenExpiry > DateTime.Now);

            if (clinicstaff == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Invalid or expired verification link." });

                TempData["ErrorMessage"] = "Invalid or expired verification link.";
                return RedirectToAction(nameof(Login));
            }

            clinicstaff.IsEmailVerified = true;
            clinicstaff.EmailVerificationToken = null;
            clinicstaff.EmailVerificationTokenExpiry = null;

            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Email verified successfully! You can now login.", redirectUrl = Url.Action(nameof(Login)) });

            TempData["SuccessMessage"] = "Email verified successfully! You can now login.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Clinicstaff/ForgotPassword
        public IActionResult ForgotPassword()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Clinicstaff/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var clinicstaff = await _context.Clinicstaffs
                        .FirstOrDefaultAsync(s => s.Email == model.Email && s.IsEmailVerified);

                    if (clinicstaff != null)
                    {
                        var resetToken = GenerateToken();
                        clinicstaff.PasswordResetToken = resetToken;
                        clinicstaff.PasswordResetTokenExpiry = DateTime.Now.AddHours(1);

                        await _context.SaveChangesAsync();

                        var baseUrl = GetBaseUrl();
                        var resetLink = $"{baseUrl}{Url.Action("ResetPassword", "Clinicstaff", new { token = resetToken, email = clinicstaff.Email })}";

                        await _emailService.SendPasswordResetEmail(clinicstaff.Email, clinicstaff.FirstName, resetLink);

                        if (IsAjaxRequest())
                            return Json(new { success = true, message = "Password reset link has been sent to your email.", redirectUrl = Url.Action(nameof(Login)) });

                        TempData["SuccessMessage"] = "Password reset link has been sent to your email.";
                        return RedirectToAction(nameof(Login));
                    }

                    // Don't reveal that the user doesn't exist or isn't verified
                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "If your email is registered and verified, you will receive a password reset link.", redirectUrl = Url.Action(nameof(Login)) });

                    TempData["SuccessMessage"] = "If your email is registered and verified, you will receive a password reset link.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Forgot password error: {ex.Message}");

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred. Please try again." });

                    TempData["ErrorMessage"] = "An error occurred. Please try again.";
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

            return View(model);
        }

        // GET: Clinicstaff/ResetPassword
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            var clinicstaff = await _context.Clinicstaffs
                .FirstOrDefaultAsync(s => s.Email == email &&
                         s.PasswordResetToken == token &&
                         s.PasswordResetTokenExpiry > DateTime.Now);

            if (clinicstaff == null)
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

        // POST: Clinicstaff/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var clinicstaff = await _context.Clinicstaffs
                        .FirstOrDefaultAsync(s => s.Email == model.Email &&
                                 s.PasswordResetToken == model.Token &&
                                 s.PasswordResetTokenExpiry > DateTime.Now);

                    if (clinicstaff == null)
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Invalid or expired reset link." });

                        TempData["ErrorMessage"] = "Invalid or expired reset link.";
                        return RedirectToAction(nameof(ForgotPassword));
                    }

                    // Update password
                    clinicstaff.Password = _passwordService.HashPassword(model.Password);
                    clinicstaff.PasswordResetToken = null;
                    clinicstaff.PasswordResetTokenExpiry = null;

                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Password reset successfully! You can now login with your new password.", redirectUrl = Url.Action(nameof(Login)) });

                    TempData["SuccessMessage"] = "Password reset successfully! You can now login with your new password.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reset password error: {ex.Message}");

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred. Please try again." });

                    TempData["ErrorMessage"] = "An error occurred. Please try again.";
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

            return View(model);
        }

        // Helper method to generate tokens
        private string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        // GET: Clinicstaff/Analytics
        [ClinicStaffOnly]
        public IActionResult Analytics()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // GET: Clinicstaff/GetAnalyticsData
        [HttpGet]
        [ClinicStaffOnly]
        public async Task<IActionResult> GetAnalyticsData(int timeRange = 30, string? chartType = null)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-timeRange);
                var endDate = DateTime.Now;

                // Get all appointments with related data
                var appointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Schedule)
                    .Where(a => a.DateBooked >= DateOnly.FromDateTime(startDate) && 
                                a.DateBooked <= DateOnly.FromDateTime(endDate))
                    .ToListAsync();

                // Get all patients
                var patients = await _context.Students
                    .Where(s => s.IsActive)
                    .ToListAsync();

                // Get patient records for demographics
                var patientRecords = await _context.Precords
                    .Include(p => p.Patient)
                    .ToListAsync();

                // Calculate appointment volume by schedule date
                var appointmentVolume = appointments
                    .Where(a => a.Schedule != null)
                    .GroupBy(a => a.Schedule.Date)
                    .Select(g => new
                    {
                        date = g.Key.ToString("MMM dd, yyyy"),
                        day = g.Key.DayOfWeek.ToString().Substring(0, 3),
                        count = g.Count()
                    })
                    .OrderBy(x => x.date)
                    .ToList();

                // Calculate age distribution from patient records
                var ageDistribution = patientRecords
                    .GroupBy(p => GetAgeGroup(p.Age))
                    .Select(g => new
                    {
                        ageGroup = g.Key,
                        count = g.Count()
                    })
                    .ToDictionary(x => x.ageGroup, x => x.count);

                // Calculate visit frequency
                var visitFrequency = appointments
                    .GroupBy(a => a.PatientId)
                    .Select(g => new
                    {
                        patientId = g.Key,
                        visitCount = g.Count()
                    })
                    .GroupBy(v => v.visitCount)
                    .Select(g => new
                    {
                        visits = g.Key.ToString(),
                        count = g.Count()
                    })
                    .ToDictionary(x => x.visits, x => x.count);

                // Calculate no-show and cancellation statistics
                var noShows = appointments.Count(a => a.AppointmentStatus == "Cancelled" && 
                    a.Schedule.Date < DateOnly.FromDateTime(DateTime.Now));
                var cancellations = appointments.Count(a => a.AppointmentStatus == "Cancelled");
                var completed = appointments.Count(a => a.AppointmentStatus == "Completed");

                // Calculate demographics stats
                var avgAge = patientRecords.Any() ? patientRecords.Average(p => (double)p.Age) : 0;
                var commonAgeGroup = ageDistribution.Any() 
                    ? ageDistribution.OrderByDescending(x => x.Value).First().Key 
                    : "N/A";

                // Calculate visit frequency stats
                var avgVisits = appointments.Any() 
                    ? appointments.GroupBy(a => a.PatientId).Average(g => g.Count()) 
                    : 0;
                var returnPatients = appointments.GroupBy(a => a.PatientId).Count(g => g.Count() > 1);

                // Get all emergency data (not just within time range for day/week/month/year stats)
                var allEmergencies = await _context.Emergencies
                    .Where(e => e.CreatedAt.HasValue)
                    .ToListAsync();

                // Calculate emergency statistics by time period
                var now = DateTime.Now;
                var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
                var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var yearStart = new DateTime(now.Year, 1, 1);

                var emergenciesToday = allEmergencies.Count(e => e.CreatedAt.Value >= todayStart);
                var emergenciesThisWeek = allEmergencies.Count(e => e.CreatedAt.Value >= weekStart);
                var emergenciesThisMonth = allEmergencies.Count(e => e.CreatedAt.Value >= monthStart);
                var emergenciesThisYear = allEmergencies.Count(e => e.CreatedAt.Value >= yearStart);

                // Get emergency data within the time range for trend chart
                var emergencies = allEmergencies
                    .Where(e => e.CreatedAt.Value >= startDate && e.CreatedAt.Value <= endDate)
                    .ToList();

                // Calculate emergency volume by date for trend chart
                var emergencyVolume = emergencies
                    .GroupBy(e => DateOnly.FromDateTime(e.CreatedAt.Value))
                    .Select(g => new
                    {
                        date = g.Key.ToString("MMM dd, yyyy"),
                        day = g.Key.DayOfWeek.ToString().Substring(0, 3),
                        count = g.Count()
                    })
                    .OrderBy(x => x.date)
                    .ToList();

                // Calculate most common reasons for visit
                var reasonsDistribution = appointments
                    .Where(a => !string.IsNullOrWhiteSpace(a.ReasonForVisit))
                    .GroupBy(a => a.ReasonForVisit)
                    .Select(g => new
                    {
                        reason = g.Key,
                        count = g.Count()
                    })
                    .OrderByDescending(x => x.count)
                    .Take(10) // Top 10 reasons
                    .ToDictionary(x => x.reason, x => x.count);

                var mostCommonReason = reasonsDistribution.Any() 
                    ? reasonsDistribution.OrderByDescending(x => x.Value).First().Key 
                    : "N/A";
                var totalUniqueReasons = appointments
                    .Where(a => !string.IsNullOrWhiteSpace(a.ReasonForVisit))
                    .Select(a => a.ReasonForVisit)
                    .Distinct()
                    .Count();

                // Return raw data for JavaScript to process
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        // Overview stats
                        totalAppointments = appointments.Count,
                        totalPatients = patients.Count,
                        noShowRate = appointments.Any() ? (noShows / (double)appointments.Count * 100) : 0,
                        avgSatisfaction = 4.2, // Placeholder - no feedback system yet

                        // Appointment volume
                        appointmentVolume = appointmentVolume,

                        // Demographics
                        ageDistribution = ageDistribution,
                        avgAge = avgAge,
                        commonAgeGroup = commonAgeGroup,

                        // Visit frequency
                        visitFrequency = visitFrequency,
                        avgVisits = avgVisits,
                        returnPatients = returnPatients,

                        // No-show and cancellation
                        noShowCancellation = new
                        {
                            noShows = noShows,
                            cancellations = cancellations,
                            completed = completed
                        },

                        // Emergency statistics
                        emergencyStatistics = new
                        {
                            emergenciesToday = emergenciesToday,
                            emergenciesThisWeek = emergenciesThisWeek,
                            emergenciesThisMonth = emergenciesThisMonth,
                            emergenciesThisYear = emergenciesThisYear,
                            emergencyVolume = emergencyVolume
                        },

                        // Reasons for visit
                        reasonsForVisit = new
                        {
                            reasonsDistribution = reasonsDistribution,
                            mostCommonReason = mostCommonReason,
                            totalUniqueReasons = totalUniqueReasons
                        },

                        // Satisfaction (placeholder - no feedback system)
                        satisfactionRatings = new Dictionary<string, int>
                        {
                            { "5", 0 },
                            { "4", 0 },
                            { "3", 0 },
                            { "2", 0 },
                            { "1", 0 }
                        },
                        totalFeedback = 0,
                        positiveFeedback = 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Helper method to get age group
        private string GetAgeGroup(int age)
        {
            if (age < 18) return "Under 18";
            if (age < 26) return "18-25";
            if (age < 36) return "26-35";
            if (age < 46) return "36-45";
            if (age < 56) return "46-55";
            if (age < 66) return "56-65";
            return "65+";
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}