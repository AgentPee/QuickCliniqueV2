using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Controllers
{
    public class ClinicstaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClinicstaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Clinicstaff
        public async Task<IActionResult> Index()
        {
            var clinicstaffs = await _context.Clinicstaffs
                .Include(c => c.User)
                .ToListAsync();
            return View(clinicstaffs);
        }

        // GET: Clinicstaff/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var clinicstaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClinicStaffId == id);

            if (clinicstaff == null)
                return NotFound();

            return View(clinicstaff);
        }

        // GET: Clinicstaff/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role");
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", clinicstaff.UserId);
            return View(clinicstaff);
        }

        // GET: Clinicstaff/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var clinicstaff = await _context.Clinicstaffs.FindAsync(id);
            if (clinicstaff == null)
                return NotFound();

            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", clinicstaff.UserId);
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
                return NotFound();

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
                        .Include(c => c.User) // Include the User navigation property
                        .FirstOrDefaultAsync(c => c.ClinicStaffId == id);

                    if (existingStaff == null)
                    {
                        Console.WriteLine("Existing staff not found in database");
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
                        existingStaff.Password = newPassword;
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

                    // Save changes - this will update both Clinicstaff and Usertype in the same transaction
                    int changes = await _context.SaveChangesAsync();
                    Console.WriteLine($"SaveChanges completed. {changes} records affected.");

                    TempData["SuccessMessage"] = "Staff member updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"DbUpdateConcurrencyException: {ex.Message}");
                    if (!ClinicstaffExists(clinicstaff.ClinicStaffId))
                    {
                        Console.WriteLine("Clinic staff no longer exists");
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
                    
                }
            }
            else
            {
                Console.WriteLine("ModelState is still invalid. Errors:");
                
            }

            // If we got this far, something failed; redisplay form
            ViewData["UserId"] = new SelectList(_context.Usertypes, "UserId", "Role", clinicstaff.UserId);
            return View(clinicstaff);
        }

        // GET: Clinicstaff/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var clinicstaff = await _context.Clinicstaffs
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClinicStaffId == id);

            if (clinicstaff == null)
                return NotFound();

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
                // Remove the clinic staff
                _context.Clinicstaffs.Remove(clinicstaff);

                // Also remove the associated user type to avoid orphaned records
                if (clinicstaff.User != null)
                {
                    _context.Usertypes.Remove(clinicstaff.User);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ClinicstaffExists(int id)
        {
            return _context.Clinicstaffs.Any(e => e.ClinicStaffId == id);
        }

        // Add these methods to your existing ClinicstaffController class

        // GET: Clinicstaff/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Clinicstaff/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = await _context.Clinicstaffs
                    .FirstOrDefaultAsync(s => s.Email == model.Email && s.Password == model.Password);

                if (staff != null)
                {
                    // Here you should implement proper authentication
                    // For production, use proper password hashing and ASP.NET Core Identity
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", "Invalid login attempt.");
            }
            return View(model);
        }

        // GET: Clinicstaff/Register
        public IActionResult Register()
        {
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
                        ModelState.AddModelError("Email", "Email already registered.");
                        return View(model);
                    }

                    // 1. Create and save the Usertype first
                    var usertype = new Usertype
                    {
                        Name = model.FirstName + " " + model.LastName,
                        Role = "ClinicStaff"
                    };

                    _context.Usertypes.Add(usertype);
                    await _context.SaveChangesAsync(); // This generates the UserId

                    Console.WriteLine($"Created Usertype with ID: {usertype.UserId}");

                    // 2. Now create the Clinicstaff with the UserId from Usertype
                    var clinicstaff = new Clinicstaff
                    {
                        UserId = usertype.UserId, // Set the foreign key
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Password = model.Password // In production, hash this!
                    };

                    // 3. Save the Clinicstaff record
                    _context.Clinicstaffs.Add(clinicstaff);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("Clinic staff registered successfully!");

                    TempData["SuccessMessage"] = "Registration successful! Please login.";
                    return RedirectToAction(nameof(Register));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during registration: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                }
            }
            else
            {
                // Log validation errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }

            return View(model);
        }

        private async Task<int> GetClinicStaffUserTypeId()
        {
            var userType = await _context.Usertypes.FirstOrDefaultAsync(u => u.Role == "ClinicStaff");
            return userType?.UserId ?? throw new InvalidOperationException("Clinic Staff user type not found");
        }
    }
}