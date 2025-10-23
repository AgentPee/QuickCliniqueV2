﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Attributes;
using System.Text;

namespace QuickClinique.Controllers
{
    public class ClinicstaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;

        public ClinicstaffController(ApplicationDbContext context, IEmailService emailService, IPasswordService passwordService)
        {
            _context = context;
            _emailService = emailService;
            _passwordService = passwordService;
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
        public async Task<IActionResult> Edit(int id, [Bind("ClinicStaffId,UserId,FirstName,LastName,Email,PhoneNumber")] Clinicstaff clinicstaff, string? newPassword)
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

        // POST: Clinicstaff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clinicstaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ClinicStaffId == id);

            if (clinicstaff != null)
            {
                _context.Clinicstaffs.Remove(clinicstaff);

                if (clinicstaff.User != null)
                {
                    _context.Usertypes.Remove(clinicstaff.User);
                }

                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Staff deleted successfully" });
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Staff not found" });
            }

            return RedirectToAction(nameof(Index));
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
            Console.WriteLine($"ModelState IsValid: {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if email already exists
                    if (await _context.Clinicstaffs.AnyAsync(x => x.Email == model.Email))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Email already registered." });

                        ModelState.AddModelError("Email", "Email already registered.");
                        return View(model);
                    }

                    // Create and save the Usertype first
                    var usertype = new Usertype
                    {
                        Name = model.FirstName + " " + model.LastName,
                        Role = "ClinicStaff"
                    };

                    _context.Usertypes.Add(usertype);
                    await _context.SaveChangesAsync();

                    // Generate email verification token
                    var emailToken = GenerateToken();

                    // Create the Clinicstaff
                    var clinicstaff = new Clinicstaff
                    {
                        UserId = usertype.UserId,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Password = _passwordService.HashPassword(model.Password),
                        IsEmailVerified = false,
                        EmailVerificationToken = emailToken,
                        EmailVerificationTokenExpiry = DateTime.Now.AddHours(24)
                    };

                    _context.Clinicstaffs.Add(clinicstaff);
                    await _context.SaveChangesAsync();

                    // Send verification email
                    var verificationLink = Url.Action("VerifyEmail", "Clinicstaff",
                        new { token = emailToken, email = clinicstaff.Email }, Request.Scheme);

                    await _emailService.SendVerificationEmail(clinicstaff.Email, clinicstaff.FirstName, verificationLink);

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Registration successful! Please check your email to verify your account.", redirectUrl = Url.Action(nameof(Login)) });

                    TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Registration error: {ex.Message}");

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred during registration. Please try again." });

                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                }
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
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

                        var resetLink = Url.Action("ResetPassword", "Clinicstaff",
                            new { token = resetToken, email = clinicstaff.Email }, Request.Scheme);

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

        public IActionResult Dashboard()
        {
            var model = new Clinicstaff();

            if (IsAjaxRequest())
                return Json(new { success = true, data = model });

            return View("clinicstaff", model);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}