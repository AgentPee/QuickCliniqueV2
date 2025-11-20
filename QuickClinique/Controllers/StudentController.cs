using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Attributes;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace QuickClinique.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StudentController(ApplicationDbContext context, IEmailService emailService, IPasswordService passwordService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _passwordService = passwordService;
            _webHostEnvironment = webHostEnvironment;
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
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Student deleted successfully" });
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
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if ID number already exists
                    if (await _context.Students.AnyAsync(x => x.Idnumber == model.Idnumber))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "ID number already registered." });

                        ModelState.AddModelError("Idnumber", "ID number already registered.");
                        return View(model);
                    }

                    // Check if email already exists
                    if (await _context.Students.AnyAsync(x => x.Email == model.Email))
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
                        Role = "Student"
                    };

                    _context.Usertypes.Add(usertype);
                    await _context.SaveChangesAsync();

                    // Generate email verification token
                    var emailToken = GenerateToken();

                    // Handle image uploads (Front and Back)
                    var imagePaths = new List<string>();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    const long maxFileSize = 5 * 1024 * 1024; // 5MB

                    // Create directory if it doesn't exist
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "student-ids");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Process Front Image
                    if (model.StudentIdImageFront != null && model.StudentIdImageFront.Length > 0)
                    {
                        var fileExtension = Path.GetExtension(model.StudentIdImageFront.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            if (IsAjaxRequest())
                                return Json(new { success = false, error = "Invalid file type for front image. Please upload an image file (jpg, jpeg, png, gif, bmp, or webp)." });

                            ModelState.AddModelError("StudentIdImageFront", "Invalid file type. Please upload an image file.");
                            return View(model);
                        }

                        if (model.StudentIdImageFront.Length > maxFileSize)
                        {
                            if (IsAjaxRequest())
                                return Json(new { success = false, error = "Front image file size exceeds the maximum limit of 5MB." });

                            ModelState.AddModelError("StudentIdImageFront", "File size exceeds the maximum limit of 5MB.");
                            return View(model);
                        }

                        var frontFileName = $"{model.Idnumber}_front_{Guid.NewGuid()}{fileExtension}";
                        var frontFilePath = Path.Combine(uploadsFolder, frontFileName);

                        using (var fileStream = new FileStream(frontFilePath, FileMode.Create))
                        {
                            await model.StudentIdImageFront.CopyToAsync(fileStream);
                        }

                        imagePaths.Add($"/img/student-ids/{frontFileName}");
                    }

                    // Process Back Image
                    if (model.StudentIdImageBack != null && model.StudentIdImageBack.Length > 0)
                    {
                        var fileExtension = Path.GetExtension(model.StudentIdImageBack.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            if (IsAjaxRequest())
                                return Json(new { success = false, error = "Invalid file type for back image. Please upload an image file (jpg, jpeg, png, gif, bmp, or webp)." });

                            ModelState.AddModelError("StudentIdImageBack", "Invalid file type. Please upload an image file.");
                            return View(model);
                        }

                        if (model.StudentIdImageBack.Length > maxFileSize)
                        {
                            if (IsAjaxRequest())
                                return Json(new { success = false, error = "Back image file size exceeds the maximum limit of 5MB." });

                            ModelState.AddModelError("StudentIdImageBack", "File size exceeds the maximum limit of 5MB.");
                            return View(model);
                        }

                        var backFileName = $"{model.Idnumber}_back_{Guid.NewGuid()}{fileExtension}";
                        var backFilePath = Path.Combine(uploadsFolder, backFileName);

                        using (var fileStream = new FileStream(backFilePath, FileMode.Create))
                        {
                            await model.StudentIdImageBack.CopyToAsync(fileStream);
                        }

                        imagePaths.Add($"/img/student-ids/{backFileName}");
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
                        EmailVerificationTokenExpiry = DateTime.Now.AddHours(24)
                    };

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // Send verification email
                    var verificationLink = Url.Action("VerifyEmail", "Student",
                        new { token = emailToken, email = student.Email }, Request.Scheme);

                    await _emailService.SendVerificationEmail(student.Email, student.FirstName, verificationLink);

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Registration successful! Please check your email to verify your account.", redirectUrl = Url.Action(nameof(Login)) });

                    TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred during registration. Please try again." });

                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
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
                            return Json(new { success = false, error = "Please verify your email before logging in." });

                        ModelState.AddModelError("", "Please verify your email before logging in.");
                        return View(model);
                    }

                    if (!student.IsActive)
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Your account has been deactivated. Please contact the clinic staff for assistance." });

                        ModelState.AddModelError("", "Your account has been deactivated. Please contact the clinic staff for assistance.");
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

                // Create directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "img", "insurance-receipts");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old file if exists
                if (!string.IsNullOrEmpty(student.InsuranceReceipt))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, student.InsuranceReceipt.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                        catch
                        {
                            // Ignore deletion errors
                        }
                    }
                }

                // Generate unique filename
                var fileName = $"{student.Idnumber}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await InsuranceReceiptFile.CopyToAsync(fileStream);
                }

                // Store relative path for database
                student.InsuranceReceipt = $"/img/insurance-receipts/{fileName}";
            }

            // Update only allowed fields
            student.FirstName = model.FirstName;
            student.LastName = model.LastName;
            student.Email = model.Email;
            student.PhoneNumber = model.PhoneNumber;
            student.Birthdate = model.Birthdate;
            student.Gender = model.Gender;

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

                    var resetLink = Url.Action("ResetPassword", "Student",
                        new { token = resetToken, email = student.Email }, Request.Scheme);

                    await _emailService.SendPasswordResetEmail(student.Email, student.FirstName, resetLink);

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

        // POST: Student/SendSOS
        [HttpPost]
        [StudentOnly]
        public async Task<IActionResult> SendSOS([FromBody] SOSRequest request)
        {
            var studentId = HttpContext.Session.GetInt32("StudentId");
            if (!studentId.HasValue)
            {
                return Json(new { success = false, error = "Not authenticated" });
            }

            if (string.IsNullOrWhiteSpace(request.Location))
            {
                return Json(new { success = false, error = "Location is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Needs))
            {
                return Json(new { success = false, error = "Please specify what you need" });
            }

            var student = await _context.Students.FindAsync(studentId.Value);
            if (student == null)
            {
                return Json(new { success = false, error = "Student not found" });
            }

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

            if (IsAjaxRequest())
                return Json(new { success = true, message = "SOS Emergency request sent successfully!", emergencyId = emergency.EmergencyId });

            TempData["SuccessMessage"] = "SOS Emergency request sent successfully!";
            return RedirectToAction(nameof(Dashboard));
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

    public class SOSRequest
    {
        public string Location { get; set; } = null!;
        public string Needs { get; set; } = null!;
    }
}