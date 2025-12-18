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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QuickClinique.Controllers
{
    public class ClinicstaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly IFileStorageService _fileStorageService;
        private readonly INotificationService _notificationService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ClinicstaffController(ApplicationDbContext context, IEmailService emailService, IPasswordService passwordService, IFileStorageService fileStorageService, INotificationService notificationService, IServiceScopeFactory serviceScopeFactory, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _passwordService = passwordService;
            _fileStorageService = fileStorageService;
            _notificationService = notificationService;
            _serviceScopeFactory = serviceScopeFactory;
            _webHostEnvironment = webHostEnvironment;
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
        public async Task<IActionResult> Edit(int id, [Bind("ClinicStaffId,UserId,FirstName,LastName,Email,PhoneNumber,LicenseNumber,Birthdate,Gender")] Clinicstaff clinicstaff, string? newPassword, string? confirmPassword)
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

            // Validate LicenseNumber format if provided
            if (!string.IsNullOrWhiteSpace(clinicstaff.LicenseNumber))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(clinicstaff.LicenseNumber.Trim(), @"^\d{2}\s\d{6}$"))
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "License number must be in format: 2 digits, space, 6 digits (e.g., 22 123456)" });
                    ModelState.AddModelError("LicenseNumber", "License number must be in format: 2 digits, space, 6 digits (e.g., 22 123456)");
                }
            }

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
                    existingStaff.LicenseNumber = clinicstaff.LicenseNumber;
                    existingStaff.Birthdate = clinicstaff.Birthdate;
                    existingStaff.Gender = clinicstaff.Gender;

                    // Update role based on LicenseNumber
                    if (existingStaff.User != null)
                    {
                        bool hasLicense = !string.IsNullOrWhiteSpace(clinicstaff.LicenseNumber);
                        bool isCurrentlyAdmin = existingStaff.User.Role?.Equals("Admin", StringComparison.OrdinalIgnoreCase) ?? false;
                        
                        if (hasLicense && !isCurrentlyAdmin)
                        {
                            // Assign Admin role if license number is provided
                            var adminUserType = await _context.Usertypes
                                .FirstOrDefaultAsync(ut => ut.Role == "Admin");
                            if (adminUserType != null)
                            {
                                existingStaff.UserId = adminUserType.UserId;
                            }
                        }
                        else if (!hasLicense && isCurrentlyAdmin)
                        {
                            // Assign ClinicStaff role if license number is removed
                            var clinicStaffUserType = await _context.Usertypes
                                .FirstOrDefaultAsync(ut => ut.Role == "ClinicStaff");
                            if (clinicStaffUserType != null)
                            {
                                existingStaff.UserId = clinicStaffUserType.UserId;
                            }
                        }
                    }

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
                var wasInactive = !clinicstaff.IsActive;
                
                // If trying to activate an account, check if email is verified first
                if (wasInactive && !clinicstaff.IsEmailVerified)
                {
                    return Json(new { 
                        success = false, 
                        message = "Cannot activate account. Staff member's email has not been verified yet. Please verify the email first before activating the account." 
                    });
                }
                
                // Toggle the IsActive status
                clinicstaff.IsActive = !clinicstaff.IsActive;
                await _context.SaveChangesAsync();

                // If activating an inactive account, send activation email
                if (wasInactive && clinicstaff.IsActive && clinicstaff.IsEmailVerified)
                {
                    var baseUrl = GetBaseUrl();
                    var loginUrl = $"{baseUrl}{Url.Action("Login", "Clinicstaff")}";

                    Console.WriteLine($"[ACTIVATION] Attempting to send activation email to {clinicstaff.Email}");
                    Console.WriteLine($"[ACTIVATION] Login URL: {loginUrl}");
                    
                    // Send email - await it but don't fail activation if email fails
                    try
                    {
                        await _emailService.SendAccountActivationEmail(clinicstaff.Email, clinicstaff.FirstName, loginUrl);
                        Console.WriteLine($"[ACTIVATION] Activation email sent successfully to {clinicstaff.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log email error but don't fail activation
                        Console.WriteLine($"[ACTIVATION ERROR] Failed to send activation email to {clinicstaff.Email}: {emailEx.Message}");
                        Console.WriteLine($"[ACTIVATION ERROR] Stack trace: {emailEx.StackTrace}");
                        if (emailEx.InnerException != null)
                        {
                            Console.WriteLine($"[ACTIVATION ERROR] Inner exception: {emailEx.InnerException.Message}");
                        }
                    }
                }

                string action = clinicstaff.IsActive ? "activated" : "deactivated";
                string message = clinicstaff.IsActive 
                    ? $"Staff member {action} successfully. Activation email sent to {clinicstaff.Email}."
                    : $"Staff member {action} successfully.";
                    
                return Json(new { success = true, message = message, isActive = clinicstaff.IsActive });
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
                            return Json(new { 
                                success = false, 
                                error = "Please verify your email before logging in.",
                                requiresVerification = true,
                                redirectUrl = Url.Action("verificationE", "Clinicstaff", new { email = staff.Email }),
                                email = staff.Email
                            });

                        TempData["ErrorMessage"] = "Please verify your email before logging in.";
                        return RedirectToAction("verificationE", "Clinicstaff", new { email = staff.Email });
                    }

                    if (!staff.IsActive)
                    {
                        // If email is verified but account is inactive, it means pending activation
                        if (IsAjaxRequest())
                            return Json(new { 
                                success = false, 
                                error = "Your account is pending activation by an administrator. You will receive an email notification once your account has been activated.",
                                pendingActivation = true
                            });

                        ModelState.AddModelError("", "Your account is pending activation by an administrator. You will receive an email notification once your account has been activated.");
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

            // Validate license number format if provided
            if (!string.IsNullOrWhiteSpace(model.LicenseNumber))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.LicenseNumber.Trim(), @"^\d{2}\s\d{6}$"))
                {
                    ModelState.AddModelError("LicenseNumber", "License number must be in format: 2 digits, space, 6 digits (e.g., 22 123456)");
                }
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

                 
                    // Determine user type based on license number
                    Usertype usertype;
                    
                    // If license number is provided, assign Admin role
                    if (!string.IsNullOrWhiteSpace(model.LicenseNumber))
                    {
                        // Find existing Admin Usertype
                        var adminUserType = await _context.Usertypes
                            .FirstOrDefaultAsync(ut => ut.Role == "Admin");
                        
                        if (adminUserType == null)
                        {
                            // Create Admin Usertype if it doesn't exist
                            adminUserType = new Usertype
                            {
                                Name = "System Administrator",
                                Role = "Admin"
                            };
                            _context.Usertypes.Add(adminUserType);
                            await _context.SaveChangesAsync();
                        }
                        
                        usertype = adminUserType;
                    }
                    else
                    {
                        // Create and save the Usertype for ClinicStaff
                        usertype = new Usertype
                        {
                            Name = model.FirstName.ToUpper() + " " + model.LastName.ToUpper(),
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
                        FirstName = model.FirstName.ToUpper(),
                        LastName = model.LastName.ToUpper(),
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Gender = model.Gender.ToUpper(),
                        Birthdate = model.Birthdate,
                        Image = imagePath,
                        Password = _passwordService.HashPassword(model.Password),
                        LicenseNumber = !string.IsNullOrWhiteSpace(model.LicenseNumber) ? model.LicenseNumber.Trim() : null,
                        IsEmailVerified = false,
                        EmailVerificationToken = emailToken,
                        EmailVerificationTokenExpiry = DateTime.Now.AddHours(24),
                        IsActive = false // Account starts as inactive until activated by administrator
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
                        return Json(new { 
                            success = true, 
                            message = "Registration successful! Please check your email to verify your account. Your account will be activated by an administrator after verification. You will receive an email once your account is activated.",
                            redirectUrl = Url.Action("verificationE", "Clinicstaff", new { email = clinicstaff.Email }),
                            clinicStaffEmail = clinicstaff.Email
                        });

                    TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account. Your account will be activated by an administrator after verification. You will receive an email once your account is activated.";
                    TempData["ClinicStaffEmail"] = clinicstaff.Email;
                    return RedirectToAction("verificationE", "Clinicstaff", new { email = clinicstaff.Email });
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

        // GET: Clinicstaff/verificationE - Display email verification page
        public IActionResult verificationE(string? email = null)
        {
            // Pass email to view if provided
            if (!string.IsNullOrEmpty(email))
            {
                ViewBag.Email = email;
            }
            else if (TempData["ClinicStaffEmail"] != null)
            {
                ViewBag.Email = TempData["ClinicStaffEmail"].ToString();
                TempData.Keep("ClinicStaffEmail");
            }

            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
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

            // Send notifications to all existing clinic staff about new staff with verified email (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    // Create a new scope for the background task
                    using var scope = _serviceScopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notificationService.NotifyNewClinicStaffAsync(clinicstaff);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[NOTIFICATION ERROR] Failed to send notification for verified clinic staff {clinicstaff.ClinicStaffId}: {ex.Message}");
                }
            });

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Email verified successfully! Your account is pending activation by an administrator. You will receive an email once your account is activated.", redirectUrl = Url.Action(nameof(Login)) });

            TempData["SuccessMessage"] = "Email verified successfully! Your account is pending activation by an administrator. You will receive an email once your account is activated.";
            return RedirectToAction(nameof(Login));
        }

        // POST: Clinicstaff/ResendVerificationEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail()
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

                var clinicstaff = await _context.Clinicstaffs
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());

                if (clinicstaff == null)
                {
                    // Don't reveal that the user doesn't exist (security best practice)
                    return Json(new { 
                        success = true, 
                        message = "If your email is registered, a verification email will be sent." 
                    });
                }

                if (clinicstaff.IsEmailVerified)
                {
                    return Json(new { 
                        success = false, 
                        error = "Your email is already verified. You can login now." 
                    });
                }

                // Generate new token and extend expiry
                var newToken = GenerateToken();
                clinicstaff.EmailVerificationToken = newToken;
                clinicstaff.EmailVerificationTokenExpiry = DateTime.Now.AddHours(24);

                await _context.SaveChangesAsync();

                // Send verification email (fire-and-forget)
                var baseUrl = GetBaseUrl();
                var verificationLink = $"{baseUrl}{Url.Action("VerifyEmail", "Clinicstaff", new { token = newToken, email = clinicstaff.Email })}";

                // Fire-and-forget: don't await, let it run in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendVerificationEmail(clinicstaff.Email, clinicstaff.FirstName, verificationLink);
                        Console.WriteLine($"[EMAIL] Verification email sent successfully to {clinicstaff.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"[EMAIL ERROR] Failed to send verification email to {clinicstaff.Email}: {emailEx.Message}");
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

        // GET: Clinicstaff/resetPasswordE - Display password reset email page
        public IActionResult resetPasswordE(string? email = null)
        {
            // Pass email to view if provided
            if (!string.IsNullOrEmpty(email))
            {
                ViewBag.Email = email;
            }
            else if (TempData["ClinicStaffEmail"] != null)
            {
                ViewBag.Email = TempData["ClinicStaffEmail"].ToString();
                TempData.Keep("ClinicStaffEmail");
            }

            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Clinicstaff/ResendPasswordResetEmail
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

                var clinicstaff = await _context.Clinicstaffs
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower() && s.IsEmailVerified);

                if (clinicstaff == null)
                {
                    // Don't reveal that the user doesn't exist (security best practice)
                    return Json(new { 
                        success = true, 
                        message = "If your email is registered and verified, a password reset email will be sent." 
                    });
                }

                // Generate new token and extend expiry
                var resetToken = GenerateToken();
                clinicstaff.PasswordResetToken = resetToken;
                clinicstaff.PasswordResetTokenExpiry = DateTime.Now.AddHours(1);

                await _context.SaveChangesAsync();

                // Send password reset email (fire-and-forget)
                var baseUrl = GetBaseUrl();
                var resetLink = $"{baseUrl}{Url.Action("ResetPassword", "Clinicstaff", new { token = resetToken, email = clinicstaff.Email })}";

                // Fire-and-forget: don't await, let it run in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendPasswordResetEmail(clinicstaff.Email, clinicstaff.FirstName, resetLink);
                        Console.WriteLine($"[EMAIL] Password reset email sent successfully to {clinicstaff.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        Console.WriteLine($"[EMAIL ERROR] Failed to send password reset email to {clinicstaff.Email}: {emailEx.Message}");
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
                            return Json(new { success = true, message = "Password reset link has been sent to your email.", redirectUrl = Url.Action("resetPasswordE", "Clinicstaff", new { email = clinicstaff.Email }) });

                        TempData["SuccessMessage"] = "Password reset link has been sent to your email.";
                        return RedirectToAction("resetPasswordE", "Clinicstaff", new { email = clinicstaff.Email });
                    }

                    // Don't reveal that the user doesn't exist or isn't verified
                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "If your email is registered and verified, you will receive a password reset link.", redirectUrl = Url.Action("resetPasswordE", "Clinicstaff", new { email = model.Email }) });

                    TempData["SuccessMessage"] = "If your email is registered and verified, you will receive a password reset link.";
                    return RedirectToAction("resetPasswordE", "Clinicstaff", new { email = model.Email });
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
            // Check if token and email are provided
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Reset link is required. Please request a new password reset link." });

                // Show the view with an error message instead of redirecting
                var errorModel = new ResetPasswordViewModel
                {
                    Token = token ?? string.Empty,
                    Email = email ?? string.Empty
                };
                ViewData["ErrorMessage"] = "Reset link is required. Please request a new password reset link from the Forgot Password page.";
                return View(errorModel);
            }

            var clinicstaff = await _context.Clinicstaffs
                .FirstOrDefaultAsync(s => s.Email == email &&
                         s.PasswordResetToken == token &&
                         s.PasswordResetTokenExpiry > DateTime.Now);

            if (clinicstaff == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Invalid or expired reset link." });

                // Show the view with an error message instead of redirecting
                var errorModel = new ResetPasswordViewModel
                {
                    Token = token,
                    Email = email
                };
                ViewData["ErrorMessage"] = "Invalid or expired reset link. Please request a new password reset link.";
                return View(errorModel);
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
                DateTime startDate;
                DateTime endDate = DateTime.Now;
                
                // Handle "Today" (timeRange = 0) by setting start date to today at 00:00:00
                if (timeRange == 0)
                {
                    startDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 0, 0, 0);
                }
                else
                {
                    startDate = DateTime.Now.AddDays(-timeRange);
                }

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

                // Get patient IDs who had appointments in the time range
                var patientIdsInRange = appointments
                    .Select(a => a.PatientId)
                    .Distinct()
                    .ToList();

                // Get students directly from Students table (always fresh from database)
                var students = await _context.Students
                    .Where(s => patientIdsInRange.Contains(s.StudentId) && s.Birthdate.HasValue)
                    .ToListAsync();

                // Calculate age distribution from Students table (always fresh from database)
                var ageDistribution = new Dictionary<string, int>();
                var today = DateOnly.FromDateTime(DateTime.Today);
                
                foreach (var student in students)
                {
                    if (!student.Birthdate.HasValue) continue;
                    
                    var birthdate = student.Birthdate.Value;
                    
                    // Skip if birthdate is in the future
                    if (birthdate > today) continue;
                    
                    // Calculate age correctly
                    var age = today.Year - birthdate.Year;
                    
                    // If birthday hasn't occurred this year yet, subtract 1
                    if (birthdate > today.AddYears(-age))
                        age--;
                    
                    // Ensure age is valid (0 or positive)
                    age = Math.Max(0, age);
                    
                    // Only count valid ages and ensure age grouping is correct
                    if (age > 0)
                    {
                        var ageGroup = GetAgeGroup(age);
                        if (ageDistribution.ContainsKey(ageGroup))
                        {
                            ageDistribution[ageGroup]++;
                        }
                        else
                        {
                            ageDistribution[ageGroup] = 1;
                        }
                    }
                }

                // Calculate gender distribution from Students table (always fresh from database)
                // Get gender directly from Students who had appointments in the time range
                var genderDistribution = new Dictionary<string, int>();
                
                // Helper function to normalize gender values
                string NormalizeGender(string? gender)
                {
                    if (string.IsNullOrWhiteSpace(gender))
                        return null;
                    
                    var normalized = gender.Trim();
                    return normalized.ToUpper() switch
                    {
                        "M" or "MALE" => "Male",
                        "F" or "FEMALE" => "Female",
                        "O" or "OTHER" => "Other",
                        _ => normalized // Keep original if it doesn't match common patterns
                    };
                }
                
                // Get gender directly from Students table for patients who had appointments in the time range
                var studentsWithGender = await _context.Students
                    .Where(s => patientIdsInRange.Contains(s.StudentId) && !string.IsNullOrWhiteSpace(s.Gender))
                    .ToListAsync();
                
                foreach (var student in studentsWithGender)
                {
                    var normalizedGender = NormalizeGender(student.Gender);
                    if (!string.IsNullOrWhiteSpace(normalizedGender))
                    {
                        if (genderDistribution.ContainsKey(normalizedGender))
                        {
                            genderDistribution[normalizedGender]++;
                        }
                        else
                        {
                            genderDistribution[normalizedGender] = 1;
                        }
                    }
                }

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

                // Calculate demographics stats (always fresh from database)
                var commonAgeGroup = ageDistribution.Any() 
                    ? ageDistribution.OrderByDescending(x => x.Value).First().Key 
                    : "N/A";
                var commonGender = genderDistribution.Any() 
                    ? genderDistribution.OrderByDescending(x => x.Value).First().Key 
                    : "N/A";
                
                // Calculate additional age statistics from Students table
                var ages = new List<int>();
                
                foreach (var student in students)
                {
                    if (!student.Birthdate.HasValue) continue;
                    
                    var birthdate = student.Birthdate.Value;
                    if (birthdate > today) continue;
                    
                    var age = today.Year - birthdate.Year;
                    if (birthdate > today.AddYears(-age))
                        age--;
                    
                    age = Math.Max(0, age);
                    if (age > 0)
                    {
                        ages.Add(age);
                    }
                }
                    
                var averageAge = ages.Any() ? Math.Round(ages.Average(), 1) : 0;
                var minAge = ages.Any() ? ages.Min() : 0;
                var maxAge = ages.Any() ? ages.Max() : 0;
                var ageRange = ages.Any() ? $"{minAge} - {maxAge}" : "N/A";
                var totalAgePatients = ages.Count;

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

                        // Demographics (always fresh from database)
                        ageDistribution = ageDistribution,
                        genderDistribution = genderDistribution,
                        commonAgeGroup = commonAgeGroup,
                        commonGender = commonGender,
                        averageAge = averageAge,
                        ageRange = ageRange,
                        totalAgePatients = totalAgePatients,

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

        // GET: Clinicstaff/Reports
        [ClinicStaffOnly]
        public IActionResult Reports()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // GET: Clinicstaff/TriageVisualization/{studentId}
        [ClinicStaffOnly]
        public async Task<IActionResult> TriageVisualization(int? studentId)
        {
            if (studentId == null)
            {
                // If no student ID provided, show list of students to select
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();
                
                return View("TriageVisualizationList", students);
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Get all triage records (Precords) for this student with timestamps
            var triageRecords = await _context.Precords
                .Where(p => p.PatientId == studentId && p.TriageDateTime.HasValue)
                .OrderBy(p => p.TriageDateTime)
                .ToListAsync();

            // Load staff information to get actual names instead of stored names
            var staffIds = triageRecords
                .Where(p => p.TriageTakenByStaffId.HasValue || p.TreatmentProvidedByStaffId.HasValue)
                .SelectMany(p => new[] { p.TriageTakenByStaffId, p.TreatmentProvidedByStaffId })
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var staffMembers = await _context.Clinicstaffs
                .Where(s => staffIds.Contains(s.ClinicStaffId))
                .ToDictionaryAsync(s => s.ClinicStaffId, s => $"{s.FirstName} {s.LastName}");

            // Update the names in memory for display
            foreach (var record in triageRecords)
            {
                if (record.TriageTakenByStaffId.HasValue && staffMembers.ContainsKey(record.TriageTakenByStaffId.Value))
                {
                    record.TriageTakenByName = staffMembers[record.TriageTakenByStaffId.Value];
                }
                if (record.TreatmentProvidedByStaffId.HasValue && staffMembers.ContainsKey(record.TreatmentProvidedByStaffId.Value))
                {
                    record.TreatmentProvidedByName = staffMembers[record.TreatmentProvidedByStaffId.Value];
                }
            }

            ViewBag.Student = student;
            ViewBag.TriageRecords = triageRecords;

            return View("TriageVisualization", student);
        }

        // GET: Clinicstaff/DownloadTriageVisualizationPdf/{studentId}
        [HttpGet]
        [ClinicStaffOnly]
        public async Task<IActionResult> DownloadTriageVisualizationPdf(int studentId)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Get all triage records (Precords) for this student with timestamps
            var triageRecords = await _context.Precords
                .Where(p => p.PatientId == studentId && p.TriageDateTime.HasValue)
                .OrderBy(p => p.TriageDateTime)
                .ToListAsync();

            if (!triageRecords.Any())
            {
                TempData["Error"] = "No triage records found for this student.";
                return RedirectToAction("TriageVisualization", new { studentId });
            }

            // Load staff information to get actual names instead of stored names
            var staffIds = triageRecords
                .Where(p => p.TriageTakenByStaffId.HasValue || p.TreatmentProvidedByStaffId.HasValue)
                .SelectMany(p => new[] { p.TriageTakenByStaffId, p.TreatmentProvidedByStaffId })
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var staffMembers = await _context.Clinicstaffs
                .Where(s => staffIds.Contains(s.ClinicStaffId))
                .ToDictionaryAsync(s => s.ClinicStaffId, s => $"{s.FirstName} {s.LastName}");

            // Update the names in memory for display
            foreach (var record in triageRecords)
            {
                if (record.TriageTakenByStaffId.HasValue && staffMembers.ContainsKey(record.TriageTakenByStaffId.Value))
                {
                    record.TriageTakenByName = staffMembers[record.TriageTakenByStaffId.Value];
                }
                if (record.TreatmentProvidedByStaffId.HasValue && staffMembers.ContainsKey(record.TreatmentProvidedByStaffId.Value))
                {
                    record.TreatmentProvidedByName = staffMembers[record.TreatmentProvidedByStaffId.Value];
                }
            }

            // Generate PDF
            QuestPDF.Settings.License = LicenseType.Community;
            
            var primaryTeal = Colors.Teal.Lighten1; // Close to #4ECDC4
            var darkTeal = Colors.Teal.Darken2; // Close to #179C8E
            
            // Calculate statistics
            var avgPulseRate = triageRecords.Where(r => r.PulseRate.HasValue).Any() 
                ? triageRecords.Where(r => r.PulseRate.HasValue).Average(r => r.PulseRate.Value) 
                : (double?)null;
            var avgTemperature = triageRecords.Where(r => r.Temperature.HasValue).Any() 
                ? triageRecords.Where(r => r.Temperature.HasValue).Average(r => (double)r.Temperature.Value) 
                : (double?)null;
            var avgOxygenSaturation = triageRecords.Where(r => r.OxygenSaturation.HasValue).Any() 
                ? triageRecords.Where(r => r.OxygenSaturation.HasValue).Average(r => r.OxygenSaturation.Value) 
                : (double?)null;
            var avgBmi = triageRecords.Any() 
                ? triageRecords.Average(r => r.Bmi) 
                : (double?)null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header Banner
                    page.Header()
                        .Height(60)
                        .Background(primaryTeal)
                        .Row(row =>
                        {
                            row.RelativeItem().PaddingLeft(15).PaddingTop(15).Column(column =>
                            {
                                column.Item().Text("Triage Data Visualization Report")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.White);
                                
                                column.Item().PaddingTop(5).Text($"{student.FirstName} {student.LastName}")
                                    .FontSize(12)
                                    .FontColor(Colors.White);
                                
                                column.Item().Text($"ID Number: {student.Idnumber}")
                                    .FontSize(10)
                                    .FontColor(Colors.White);
                            });
                            
                            // Right side - Logo
                            row.RelativeItem().AlignRight().PaddingRight(15).PaddingTop(10).Column(rightColumn =>
                            {
                                var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "logo.png");
                                if (System.IO.File.Exists(logoPath))
                                {
                                    rightColumn.Item().Width(40).Height(40).Image(logoPath);
                                }
                            });
                        });

                    page.Content()
                        .PaddingVertical(15)
                        .Column(column =>
                        {
                            // Statistics Summary
                            column.Item().Text("Statistics Summary").FontSize(14).Bold().FontColor(darkTeal);
                            column.Item().PaddingTop(10).Row(row =>
                            {
                                row.ConstantItem(120).Text("Total Records:").FontSize(10);
                                row.RelativeItem().Text(triageRecords.Count.ToString()).FontSize(10).Bold();
                            });
                            
                            if (avgPulseRate.HasValue)
                            {
                                column.Item().PaddingTop(5).Row(row =>
                                {
                                    row.ConstantItem(120).Text("Avg Pulse Rate:").FontSize(10);
                                    row.RelativeItem().Text($"{avgPulseRate.Value:F1} bpm").FontSize(10).Bold();
                                });
                            }
                            
                            if (avgTemperature.HasValue)
                            {
                                column.Item().PaddingTop(5).Row(row =>
                                {
                                    row.ConstantItem(120).Text("Avg Temperature:").FontSize(10);
                                    row.RelativeItem().Text($"{avgTemperature.Value:F1} °C").FontSize(10).Bold();
                                });
                            }
                            
                            if (avgOxygenSaturation.HasValue)
                            {
                                column.Item().PaddingTop(5).Row(row =>
                                {
                                    row.ConstantItem(120).Text("Avg O2 Saturation:").FontSize(10);
                                    row.RelativeItem().Text($"{avgOxygenSaturation.Value:F1}%").FontSize(10).Bold();
                                });
                            }
                            
                            if (avgBmi.HasValue)
                            {
                                column.Item().PaddingTop(5).Row(row =>
                                {
                                    row.ConstantItem(120).Text("Avg BMI:").FontSize(10);
                                    row.RelativeItem().Text($"{avgBmi.Value:F1}").FontSize(10).Bold();
                                });
                            }

                            column.Item().PaddingTop(20);

                            // Detailed Records Table
                            column.Item().Text("Detailed Triage Records").FontSize(14).Bold().FontColor(darkTeal);
                            column.Item().PaddingTop(10).Table(table =>
                            {
                                // Header
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1.2f);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Date & Time").Bold();
                                    header.Cell().Element(CellStyle).Text("Pulse Rate").Bold();
                                    header.Cell().Element(CellStyle).Text("Blood Pressure").Bold();
                                    header.Cell().Element(CellStyle).Text("Temperature").Bold();
                                    header.Cell().Element(CellStyle).Text("O2 Saturation").Bold();
                                    header.Cell().Element(CellStyle).Text("BMI").Bold();
                                    header.Cell().Element(CellStyle).Text("Taken By").Bold();

                                    IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .Background(primaryTeal)
                                            .Padding(8)
                                            .Border(1)
                                            .BorderColor(Colors.White)
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.White));
                                    }
                                });

                                // Data rows
                                foreach (var record in triageRecords)
                                {
                                    table.Cell().Element(CellStyle).Text(record.TriageDateTime?.ToString("MMM dd, yyyy\nHH:mm") ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(record.PulseRate?.ToString() ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(record.BloodPressure ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(record.Temperature?.ToString("F1") ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(record.OxygenSaturation?.ToString() ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(record.Bmi.ToString());
                                    table.Cell().Element(CellStyle).Text(record.TriageTakenByName ?? "N/A");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .Padding(6)
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .DefaultTextStyle(x => x.FontSize(8));
                                    }
                                }
                            });

                            column.Item().PaddingTop(20);
                            column.Item().Text($"Generated on: {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1)
                                .Italic();
                        });

                    // Footer Banner
                    page.Footer()
                        .Height(30)
                        .Background(primaryTeal)
                        .AlignCenter()
                        .PaddingTop(8)
                        .Text("QuickClinique - University of Cebu Medical-Dental Clinic")
                        .FontSize(9)
                        .FontColor(Colors.White);
                });
            });

            try
            {
                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"Triage_Visualization_{student.FirstName}_{student.LastName}_{DateTime.Now:yyyyMMdd}.pdf";
                // File() method automatically sets Content-Disposition header when filename is provided
                return File(stream, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Log the error (you might want to use a logger here)
                TempData["Error"] = $"An error occurred while generating the PDF: {ex.Message}";
                return RedirectToAction("TriageVisualization", new { studentId });
            }
        }

        // GET: Clinicstaff/ForensicReport/{studentId?}
        [ClinicStaffOnly]
        public async Task<IActionResult> ForensicReport(int? studentId)
        {
            if (studentId == null)
            {
                // If no student ID provided, show list of students to select
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();
                
                return View("ForensicReportList", students);
            }

            var student = await _context.Students
                .Include(s => s.Precords)
                .Include(s => s.Appointments)
                    .ThenInclude(a => a.Schedule)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Get all triage records with timestamps
            var triageRecords = student.Precords
                .Where(p => p.TriageDateTime.HasValue)
                .OrderBy(p => p.TriageDateTime)
                .ToList();

            // Load staff information to get actual names instead of stored names
            var staffIds = triageRecords
                .Where(p => p.TriageTakenByStaffId.HasValue || p.TreatmentProvidedByStaffId.HasValue)
                .SelectMany(p => new[] { p.TriageTakenByStaffId, p.TreatmentProvidedByStaffId })
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct()
                .ToList();

            var staffMembers = await _context.Clinicstaffs
                .Where(s => staffIds.Contains(s.ClinicStaffId))
                .ToDictionaryAsync(s => s.ClinicStaffId, s => $"{s.FirstName} {s.LastName}");

            // Update the names in memory for display
            foreach (var record in triageRecords)
            {
                if (record.TriageTakenByStaffId.HasValue && staffMembers.ContainsKey(record.TriageTakenByStaffId.Value))
                {
                    record.TriageTakenByName = staffMembers[record.TriageTakenByStaffId.Value];
                }
                if (record.TreatmentProvidedByStaffId.HasValue && staffMembers.ContainsKey(record.TreatmentProvidedByStaffId.Value))
                {
                    record.TreatmentProvidedByName = staffMembers[record.TreatmentProvidedByStaffId.Value];
                }
            }

            // Calculate statistics
            var stats = new
            {
                TotalVisits = student.Appointments.Count(a => a.AppointmentStatus == "Completed"),
                TotalRecords = student.Precords.Count,
                AveragePulseRate = triageRecords.Where(r => r.PulseRate.HasValue).Any() 
                    ? triageRecords.Where(r => r.PulseRate.HasValue).Average(r => r.PulseRate.Value) : (double?)null,
                AverageTemperature = triageRecords.Where(r => r.Temperature.HasValue).Any()
                    ? triageRecords.Where(r => r.Temperature.HasValue).Average(r => (double)r.Temperature.Value) : (double?)null,
                AverageOxygenSaturation = triageRecords.Where(r => r.OxygenSaturation.HasValue).Any()
                    ? triageRecords.Where(r => r.OxygenSaturation.HasValue).Average(r => r.OxygenSaturation.Value) : (double?)null,
                MostCommonDiagnosis = student.Precords
                    .Where(p => !string.IsNullOrEmpty(p.Diagnosis) && p.Diagnosis != "Triage in progress - Diagnosis pending")
                    .GroupBy(p => p.Diagnosis)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key,
                DateRange = triageRecords.Any() 
                    ? new { 
                        Start = triageRecords.Min(r => r.TriageDateTime), 
                        End = triageRecords.Max(r => r.TriageDateTime) 
                    } : null
            };

            ViewBag.Student = student;
            ViewBag.TriageRecords = triageRecords;
            ViewBag.Statistics = stats;

            return View("ForensicReport", student);
        }

        // GET: Clinicstaff/DoctorsReport
        [ClinicStaffOnly]
        public async Task<IActionResult> DoctorsReport(int? studentId)
        {
            if (studentId == null)
            {
                // If no student ID provided, show list of students to select
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();
                
                return View("DoctorsReportList", students);
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Get current clinic staff member
            var staffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (staffId.HasValue)
            {
                var staff = await _context.Clinicstaffs
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.ClinicStaffId == staffId.Value);

                if (staff != null)
                {
                    ViewBag.ClinicStaffName = $"{staff.FirstName} {staff.LastName}";
                    ViewBag.ClinicStaffRole = staff.User?.Role ?? "Admin";
                }
            }
            
            return View("DoctorsReportForm", student);
        }

        // GET: Clinicstaff/StudentCertificationReport
        [ClinicStaffOnly]
        public async Task<IActionResult> StudentCertificationReport(int? studentId)
        {
            if (studentId == null)
            {
                // If no student ID provided, show list of students to select
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToListAsync();
                
                return View("StudentCertificationReportList", students);
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound();
            }

            ViewBag.StudentId = studentId;
            ViewBag.StudentName = $"{student.FirstName} {student.LastName}";
            
            return View("MedicalCertificateForm", student);
        }

        // POST: Clinicstaff/GenerateMedicalCertificate
        [HttpPost]
        [ClinicStaffOnly]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMedicalCertificate(int studentId, string diagnosis, DateOnly fromDate, DateOnly untilDate, string? remarks, int recommendedDaysToRest)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Get current clinic staff member
            var staffId = HttpContext.Session.GetInt32("ClinicStaffId");
            if (!staffId.HasValue)
            {
                return Unauthorized();
            }

            var staff = await _context.Clinicstaffs
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.ClinicStaffId == staffId.Value);

            if (staff == null)
            {
                return Unauthorized();
            }

            // Generate PDF
            QuestPDF.Settings.License = LicenseType.Community;
            
            // QuickClinique theme colors - using Colors.Teal with modifiers for closest match
            // #4ECDC4 is close to Teal, #179C8E is close to Teal Darken2
            var primaryTeal = Colors.Teal.Lighten1; // Close to #4ECDC4
            var darkTeal = Colors.Teal.Darken2; // Close to #179C8E
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    // Header with QuickClinique Branding and Logo
                    page.Header()
                        .Height(80)
                        .Background(primaryTeal)
                        .Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                // Left side - QuickClinique text
                                row.RelativeItem().PaddingLeft(20).PaddingTop(20).Column(leftColumn =>
                                {
                                    leftColumn.Item().Text("QuickClinique")
                                        .FontSize(20)
                                        .Bold()
                                        .FontColor(Colors.White);
                                });
                                
                                // Right side - Logo
                                row.RelativeItem().AlignRight().PaddingRight(20).PaddingTop(10).Column(rightColumn =>
                                {
                                    var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "logo.png");
                                    if (System.IO.File.Exists(logoPath))
                                    {
                                        rightColumn.Item().Width(50).Height(50).Image(logoPath);
                                    }
                                    else
                                    {
                                        // Fallback if logo doesn't exist
                                        rightColumn.Item().Text("QC")
                                            .FontSize(24)
                                            .Bold()
                                            .FontColor(Colors.White);
                                    }
                                });
                            });
                        });

                    page.Content()
                        .PaddingVertical(30)
                        .Column(column =>
                        {
                            // Hospital Name - Centered
                            column.Item().AlignCenter().Text("University of Cebu Medical-Dental Clinic")
                                .FontSize(16)
                                .Bold()
                                .FontColor(Colors.Black);

                            // Title - Centered
                            column.Item().PaddingTop(20).AlignCenter().Text("MEDICAL CERTIFICATE")
                                .FontSize(28)
                                .Bold()
                                .FontColor(Colors.Black);

                            // Introductory Text - Centered
                            column.Item().PaddingTop(30).AlignCenter().Text("This is to confirm that")
                                .FontSize(12)
                                .FontColor(Colors.Black);

                            // Patient Name - Centered, Theme Color, Bold
                            column.Item().PaddingTop(15).AlignCenter().Text($"{student.FirstName} {student.LastName}")
                                .FontSize(20)
                                .Bold()
                                .FontColor(primaryTeal);

                            // Diagnosis and Recommendation - Centered
                            var diagnosisText = !string.IsNullOrEmpty(diagnosis) ? diagnosis : "N/A";
                            var recommendationText = $"is diagnosed with {diagnosisText}. ";
                            recommendationText += $"He/She is advised bed rest for {recommendedDaysToRest} day{(recommendedDaysToRest > 1 ? "s" : "")}";
                            recommendationText += $" from {fromDate.ToString("MMMM dd, yyyy")} until {untilDate.ToString("MMMM dd, yyyy")}.";

                            column.Item().PaddingTop(20).AlignCenter().DefaultTextStyle(x => x.FontSize(12).FontColor(Colors.Black).LineHeight(1.6f)).Text(recommendationText);

                            // Issuance Date - Centered
                            column.Item().PaddingTop(25).AlignCenter().Text($"Issued on {DateTime.Now.ToString("MMMM dd, yyyy")}.")
                                .FontSize(11)
                                .FontColor(Colors.Black);

                            // Doctor's Signature Block - Left Aligned
                            column.Item().PaddingTop(50).Row(row =>
                            {
                                row.RelativeItem(2).Column(sigColumn =>
                                {
                                    // Signature line
                                    sigColumn.Item().Width(150).LineHorizontal(1).LineColor(Colors.Black);
                                    
                                    // Doctor's name
                                    sigColumn.Item().PaddingTop(8).Text($"{staff.FirstName} {staff.LastName}")
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Black);
                                    
                                    // Doctor's title
                                    var role = staff.User?.Role ?? "Attending Physician";
                                    sigColumn.Item().PaddingTop(2).Text(role)
                                        .FontSize(11)
                                        .FontColor(Colors.Black);
                                });
                            });
                        });

                    // Footer - Colored Band (Template Style)
                    page.Footer()
                        .Height(40)
                        .Background(primaryTeal)
                        .Column(column =>
                        {
                            // Optional: Add decorative elements or branding
                            column.Item().PaddingTop(10).AlignCenter().Text(text =>
                            {
                                text.Span("QuickClinique").FontSize(10).Bold().FontColor(Colors.White);
                                text.Span(" - ").FontSize(10).FontColor(Colors.White);
                                text.Span("University of Cebu Medical-Dental Clinic").FontSize(9).FontColor(Colors.White);
                            });
                        });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            var fileName = $"Medical_Certificate_{student.FirstName}_{student.LastName}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(stream, "application/pdf", fileName);
        }

        // POST: Clinicstaff/GenerateDoctorsReport
        [HttpPost]
        [ClinicStaffOnly]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDoctorsReport(
            int studentId,
            string? initialDiagnosis,
            bool requiresSurgery,
            string[]? medicalHistory,
            bool noSurgeries,
            string[]? surgeryHospital,
            string[]? surgeryYear,
            string[]? surgeryComplications,
            bool noAllergies,
            string? allergies,
            bool noMedications,
            string? medications,
            string? additionalNotes,
            string? signatureHonorific,
            string? signatureName,
            string? signatureRole)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Generate PDF
            QuestPDF.Settings.License = LicenseType.Community;
            
            var primaryTeal = Colors.Teal.Lighten1; // Close to #4ECDC4
            var reportDate = DateTime.Now;
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.8f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header Banner
                    page.Header()
                        .Height(50)
                        .Background(primaryTeal)
                        .Row(row =>
                        {
                            row.RelativeItem().PaddingLeft(15).PaddingTop(12).Text("QuickClinique")
                                .FontSize(16)
                                .Bold()
                                .FontColor(Colors.White);
                            
                            // Right side - Logo
                            row.RelativeItem().AlignRight().PaddingRight(15).PaddingTop(5).Column(rightColumn =>
                            {
                                var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "img", "logo.png");
                                if (System.IO.File.Exists(logoPath))
                                {
                                    rightColumn.Item().Width(40).Height(40).Image(logoPath);
                                }
                            });
                        });

                    page.Content()
                        .PaddingVertical(10)
                        .Column(column =>
                        {
                            // Clinic Title
                            column.Item().AlignCenter().Text("University of Cebu Medical-Dental Clinic")
                                .FontSize(12)
                                .Bold();
                            column.Item().AlignCenter().PaddingTop(5).Text("Doctor's Report")
                                .FontSize(18)
                                .Bold();

                            column.Item().PaddingTop(10);

                            // Patient Information
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Patient's Name: ").Bold();
                                    text.Span($"{student.FirstName} {student.LastName}");
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Initial Diagnosis: ").Bold();
                                    text.Span(initialDiagnosis ?? "");
                                });
                            });

                            column.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("DOB: ").Bold();
                                    text.Span(student.Birthdate?.ToString("MMMM dd, yyyy") ?? "");
                                });
                                row.RelativeItem().Text(text =>
                                {
                                    text.Span("Date: ").Bold();
                                    text.Span(reportDate.ToString("MMMM dd, yyyy"));
                                });
                            });

                            column.Item().PaddingTop(8).Text(text =>
                            {
                                text.Span("Does the issue require surgery? ").Bold();
                                text.Span(requiresSurgery ? "☐ No  ☑ Yes" : "☑ No  ☐ Yes");
                            });

                            column.Item().PaddingTop(10);

                            // Medical History Section
                            column.Item().Background(Colors.Teal.Lighten5)
                                .Border(2)
                                .BorderColor(primaryTeal)
                                .Padding(8)
                                .Column(historyColumn =>
                                {
                                    historyColumn.Item().Text("Medical History: Do you have a history of the following problems?")
                                        .Bold()
                                        .FontSize(10);

                                    historyColumn.Item().PaddingTop(5).Row(historyRow =>
                                    {
                                        // Left column
                                        historyRow.RelativeItem().Column(leftCol =>
                                        {
                                            var leftConditions = new[] { "Asthma", "Diabetes", "Heart Problems", "Cancer", "Stroke", "Bone/joint problems", "Kidney problems", "Gallbladder", "Liver problems" };
                                            foreach (var condition in leftConditions)
                                            {
                                                var isChecked = medicalHistory?.Contains(condition) ?? false;
                                                leftCol.Item().PaddingBottom(2).Text(text =>
                                                {
                                                    text.Span(isChecked ? "☑ " : "☐ ").FontSize(9);
                                                    text.Span(condition).FontSize(9);
                                                });
                                            }
                                        });

                                        // Right column
                                        historyRow.RelativeItem().PaddingLeft(20).Column(rightCol =>
                                        {
                                            var rightConditions = new[] { "Electrical implants", "Anxiety attacks", "Sleep apnea", "Depression", "Bowel problems", "Alcohol abuse", "Drug use", "Smoking", "Headaches" };
                                            foreach (var condition in rightConditions)
                                            {
                                                var isChecked = medicalHistory?.Contains(condition) ?? false;
                                                rightCol.Item().PaddingBottom(2).Text(text =>
                                                {
                                                    text.Span(isChecked ? "☑ " : "☐ ").FontSize(9);
                                                    text.Span(condition).FontSize(9);
                                                });
                                            }
                                        });
                                    });
                                });

                            column.Item().PaddingTop(8);

                            // Past Surgeries
                            column.Item().Text("Past Surgeries:").Bold();
                            
                            // Only show "No surgeries" if checked
                            if (noSurgeries)
                            {
                                column.Item().PaddingTop(2).Text("☑ No surgeries");
                            }
                            else if (surgeryHospital != null && surgeryHospital.Length > 0)
                            {
                                // Show surgery details for each entry
                                for (int i = 0; i < surgeryHospital.Length; i++)
                                {
                                    var hospital = surgeryHospital[i] ?? "";
                                    var year = (surgeryYear != null && i < surgeryYear.Length) ? surgeryYear[i] ?? "" : "";
                                    var complications = (surgeryComplications != null && i < surgeryComplications.Length) ? surgeryComplications[i] ?? "" : "";
                                    
                                    if (!string.IsNullOrWhiteSpace(hospital) || !string.IsNullOrWhiteSpace(year) || !string.IsNullOrWhiteSpace(complications))
                                    {
                                        column.Item().PaddingTop(i > 0 ? 5 : 3).Row(surgeryRow =>
                                        {
                                            surgeryRow.RelativeItem().Text($"hospital: {hospital}");
                                            surgeryRow.RelativeItem().Text($"Year: {year}");
                                            surgeryRow.RelativeItem().Text($"Complications: {complications}");
                                        });
                                    }
                                }
                            }

                            column.Item().PaddingTop(8);

                            // Allergies and Medications
                            column.Item().Row(medRow =>
                            {
                                medRow.RelativeItem().Border(2).BorderColor(primaryTeal).Padding(6).Column(allergyCol =>
                                {
                                    allergyCol.Item().Text("Allergies (list down allergies you have)").Bold().FontSize(9);
                                    if (noAllergies)
                                    {
                                        allergyCol.Item().PaddingTop(2).Text("☑ No allergies");
                                    }
                                    else
                                    {
                                        allergyCol.Item().PaddingTop(3).Text(allergies ?? "");
                                    }
                                });
                                
                                medRow.RelativeItem().PaddingLeft(10).Border(2).BorderColor(primaryTeal).Padding(6).Column(medCol =>
                                {
                                    medCol.Item().Text("Medications (and dosage)").Bold().FontSize(9);
                                    if (noMedications)
                                    {
                                        medCol.Item().PaddingTop(2).Text("☑ No medications");
                                    }
                                    else
                                    {
                                        medCol.Item().PaddingTop(2).Text(medications ?? "");
                                    }
                                });
                            });

                            column.Item().PaddingTop(8);

                            // Additional Notes
                            column.Item().Text("Additional Notes:").Bold();
                            column.Item().Border(2).BorderColor(primaryTeal).Padding(6).Text(additionalNotes ?? "");

                            column.Item().PaddingTop(20);

                            // Signature Section
                            column.Item().Width(250).PaddingTop(30).LineHorizontal(1);
                            var fullSignatureName = !string.IsNullOrWhiteSpace(signatureHonorific) && !string.IsNullOrWhiteSpace(signatureName)
                                ? $"{signatureHonorific} {signatureName?.ToUpper()}"
                                : signatureName?.ToUpper() ?? "";
                            column.Item().PaddingTop(5).Text(fullSignatureName).Bold();
                            column.Item().PaddingTop(2).Text(signatureRole ?? "").FontSize(9);
                        });

                    // Footer Banner
                    page.Footer()
                        .Height(30)
                        .Background(primaryTeal)
                        .AlignCenter()
                        .PaddingTop(8)
                        .Text("QuickClinique - University of Cebu Medical-Dental Clinic")
                        .FontSize(9)
                        .FontColor(Colors.White);
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            var fileName = $"Doctors_Report_{student.FirstName}_{student.LastName}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(stream, "application/pdf", fileName);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}