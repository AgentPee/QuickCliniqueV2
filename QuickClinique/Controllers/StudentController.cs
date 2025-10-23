using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using QuickClinique.Attributes;
using System.Text;

namespace QuickClinique.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordService _passwordService;

        public StudentController(ApplicationDbContext context, IEmailService emailService, IPasswordService passwordService)
        {
            _context = context;
            _emailService = emailService;
            _passwordService = passwordService;
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

                    // Create the Student
                    var student = new Student
                    {
                        UserId = usertype.UserId,
                        Idnumber = model.Idnumber,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
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

                    // Set session or authentication cookie
                    HttpContext.Session.SetInt32("StudentId", student.StudentId);
                    HttpContext.Session.SetString("StudentName", student.FirstName + " " + student.LastName);

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Login successful!", redirectUrl = Url.Action("Index", "Home") });

                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Index", "Home");
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
}