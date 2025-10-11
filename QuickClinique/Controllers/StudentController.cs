using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;
using System.Text;

namespace QuickClinique.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public StudentController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Student
        public async Task<IActionResult> Index()
        {
            var students = _context.Students
                .Include(s => s.User);
            return View(await students.ToListAsync());
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // GET: Student/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role");
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", student.UserId);
            return View(student);
        }

        // GET: Student/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound();

            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", student.UserId);
            return View(student);
        }

        // POST: Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,UserId,Idnumber,FirstName,LastName,Email,Password,PhoneNumber")] Student student)
        {
            if (id != student.StudentId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", student.UserId);
            return View(student);
        }

        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null)
                return NotFound();

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
                        ModelState.AddModelError("Idnumber", "ID number already registered.");
                        return View(model);
                    }

                    // Check if email already exists
                    if (await _context.Students.AnyAsync(x => x.Email == model.Email))
                    {
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
                        Password = model.Password, // In production, hash this!
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

                    TempData["SuccessMessage"] = "Registration successful! Please check your email to verify your account.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                }
            }
            return View(model);
        }

        // GET: Student/Login
        public IActionResult Login()
        {
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
                    .FirstOrDefaultAsync(s => s.Idnumber == model.Idnumber && s.Password == model.Password);

                if (student != null)
                {
                    if (!student.IsEmailVerified)
                    {
                        ModelState.AddModelError("", "Please verify your email before logging in.");
                        return View(model);
                    }

                    // Set session or authentication cookie
                    HttpContext.Session.SetInt32("StudentId", student.StudentId);
                    HttpContext.Session.SetString("StudentName", student.FirstName + " " + student.LastName);

                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid ID number or password.");
            }
            return View(model);
        }

        // GET: Student/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
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
                TempData["ErrorMessage"] = "Invalid or expired verification link.";
                return RedirectToAction(nameof(Login));
            }

            student.IsEmailVerified = true;
            student.EmailVerificationToken = null;
            student.EmailVerificationTokenExpiry = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Email verified successfully! You can now login.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Student/ForgotPassword
        public IActionResult ForgotPassword()
        {
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

                    TempData["SuccessMessage"] = "Password reset link has been sent to your email.";
                    return RedirectToAction(nameof(Login));
                }

                // Don't reveal that the user doesn't exist or isn't verified
                TempData["SuccessMessage"] = "If your email is registered and verified, you will receive a password reset link.";
                return RedirectToAction(nameof(Login));
            }
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
                TempData["ErrorMessage"] = "Invalid or expired reset link.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

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
                    TempData["ErrorMessage"] = "Invalid or expired reset link.";
                    return RedirectToAction(nameof(ForgotPassword));
                }

                // Update password
                student.Password = model.Password; // In production, hash this!
                student.PasswordResetToken = null;
                student.PasswordResetTokenExpiry = null;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Password reset successfully! You can now login with your new password.";
                return RedirectToAction(nameof(Login));
            }
            return View(model);
        }

        // Helper method to generate tokens
        private string GenerateToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
}
    
