using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Schedule
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.Schedules.ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = schedules });

            return View(schedules);
        }

        // GET: Schedule/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.Appointments)
                .Include(s => s.Histories)
                .FirstOrDefaultAsync(m => m.ScheduleId == id);

            if (schedule == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Schedule not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = schedule });

            return View(schedule);
        }

        // GET: Schedule/Create
        public IActionResult Create()
        {
            if (IsAjaxRequest())
                return Json(new { success = true });

            return View();
        }

        // POST: Schedule/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Date,StartTime,EndTime,IsAvailable")] Schedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(schedule);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Schedule created successfully", id = schedule.ScheduleId });

                return RedirectToAction(nameof(Index));
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

            return View(schedule);
        }

        // GET: Schedule/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Schedule not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = schedule });

            return View(schedule);
        }

        // POST: Schedule/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ScheduleId,Date,StartTime,EndTime,IsAvailable")] Schedule schedule)
        {
            if (id != schedule.ScheduleId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Schedule updated successfully" });

                    return RedirectToAction(nameof(Availability));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.ScheduleId))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Schedule not found" });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
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

            return View(schedule);
        }

        // GET: Schedule/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var schedule = await _context.Schedules
                .FirstOrDefaultAsync(m => m.ScheduleId == id);

            if (schedule == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Schedule not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = schedule });

            return View(schedule);
        }

        // POST: Schedule/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Schedule deleted successfully" });
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Schedule not found" });
            }

            return RedirectToAction(nameof(Availability));
        }

        private bool ScheduleExists(int id)
        {
            return _context.Schedules.Any(e => e.ScheduleId == id);
        }

        // GET: Schedule/CreateMultiple
        public IActionResult CreateMultiple()
        {
            var model = new ScheduleBulkCreateViewModel();

            if (IsAjaxRequest())
                return Json(new { success = true, data = model });

            return View(model);
        }

        // POST: Schedule/CreateMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMultiple(ScheduleBulkCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var schedules = new List<Schedule>();
                    var currentDate = model.StartDate;

                    // Generate schedules for each day in the range
                    while (currentDate <= model.EndDate)
                    {
                        // Check if we should create schedule for this day of week
                        if (ShouldCreateForDay(currentDate, model.SelectedDays))
                        {
                            var schedule = new Schedule
                            {
                                Date = currentDate,
                                StartTime = model.StartTime,
                                EndTime = model.EndTime,
                                IsAvailable = model.IsAvailable
                            };
                            schedules.Add(schedule);
                        }
                        currentDate = currentDate.AddDays(1);
                    }

                    if (schedules.Count > 0)
                    {
                        // Add all schedules to context
                        _context.Schedules.AddRange(schedules);
                        await _context.SaveChangesAsync();

                        if (IsAjaxRequest())
                            return Json(new { success = true, message = $"Successfully created {schedules.Count} schedule(s)!" });

                        TempData["SuccessMessage"] = $"Successfully created {schedules.Count} schedule(s)!";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "No schedules were created. Please check your date range and selected days." });

                        ModelState.AddModelError("", "No schedules were created. Please check your date range and selected days.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating multiple schedules: {ex.Message}");

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred while creating schedules. Please try again." });

                    ModelState.AddModelError("", "An error occurred while creating schedules. Please try again.");
                }
            }

            // If we got here, something went wrong - redisplay form
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

        // GET: Schedule/CreateQuick
        public IActionResult CreateQuick()
        {
            var model = new ScheduleQuickCreateViewModel();

            if (IsAjaxRequest())
                return Json(new { success = true, data = model });

            return View(model);
        }

        // POST: Schedule/CreateQuick
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuick(ScheduleQuickCreateViewModel model)
        {
            if (ModelState.IsValid && model.SelectedDates != null && model.SelectedDates.Any())
            {
                try
                {
                    var schedules = new List<Schedule>();

                    foreach (var date in model.SelectedDates)
                    {
                        var schedule = new Schedule
                        {
                            Date = date,
                            StartTime = model.StartTime,
                            EndTime = model.EndTime,
                            IsAvailable = model.IsAvailable
                        };
                        schedules.Add(schedule);
                    }

                    _context.Schedules.AddRange(schedules);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = $"Successfully created {schedules.Count} schedule(s)!" });

                    TempData["SuccessMessage"] = $"Successfully created {schedules.Count} schedule(s)!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating quick schedules: {ex.Message}");

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = "An error occurred while creating schedules. Please try again." });

                    ModelState.AddModelError("", "An error occurred while creating schedules. Please try again.");
                }
            }
            else if (model.SelectedDates == null || !model.SelectedDates.Any())
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Please select at least one date." });

                ModelState.AddModelError("SelectedDates", "Please select at least one date.");
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

        // Helper method to check if schedule should be created for specific day
        private bool ShouldCreateForDay(DateOnly date, List<string>? selectedDays)
        {
            if (selectedDays == null || !selectedDays.Any())
                return true;

            var dayOfWeek = date.DayOfWeek.ToString();
            return selectedDays.Contains(dayOfWeek);
        }

        // GET: Schedule/Availability
        public async Task<IActionResult> Availability(string status = "All", DateOnly? startDate = null, DateOnly? endDate = null)
        {
            ViewData["CurrentFilter"] = status;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            var schedules = _context.Schedules.AsQueryable();

            // Filter by availability status
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                schedules = schedules.Where(s => s.IsAvailable == status);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                schedules = schedules.Where(s => s.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                schedules = schedules.Where(s => s.Date <= endDate.Value);
            }

            // Order by date and time
            schedules = schedules.OrderBy(s => s.Date)
                                .ThenBy(s => s.StartTime);

            var model = await schedules.ToListAsync();

            // Calculate statistics
            ViewBag.TotalSchedules = await _context.Schedules.CountAsync();
            ViewBag.AvailableSchedules = await _context.Schedules.CountAsync(s => s.IsAvailable == "Yes");
            ViewBag.UnavailableSchedules = await _context.Schedules.CountAsync(s => s.IsAvailable == "No");

            if (IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    data = model,
                    statistics = new
                    {
                        total = ViewBag.TotalSchedules,
                        available = ViewBag.AvailableSchedules,
                        unavailable = ViewBag.UnavailableSchedules
                    }
                });

            return View(model);
        }

        // GET: Schedule/Available
        public async Task<IActionResult> Available()
        {
            var availableSchedules = await _context.Schedules
                .Where(s => s.IsAvailable == "Yes")
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.FilterType = "Available";

            if (IsAjaxRequest())
                return Json(new { success = true, data = availableSchedules, filterType = "Available" });

            return View("Availability", availableSchedules);
        }

        // GET: Schedule/Unavailable
        public async Task<IActionResult> Unavailable()
        {
            var unavailableSchedules = await _context.Schedules
                .Where(s => s.IsAvailable == "No")
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.FilterType = "Unavailable";

            if (IsAjaxRequest())
                return Json(new { success = true, data = unavailableSchedules, filterType = "Unavailable" });

            return View("Availability", unavailableSchedules);
        }

        // POST: Schedule/BulkUpdateAvailability
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateAvailability(int[] scheduleIds, string newAvailability)
        {
            if (scheduleIds == null || !scheduleIds.Any())
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "No schedules selected for update." });

                TempData["ErrorMessage"] = "No schedules selected for update.";
                return RedirectToAction(nameof(Availability));
            }

            try
            {
                var schedules = await _context.Schedules
                    .Where(s => scheduleIds.Contains(s.ScheduleId))
                    .ToListAsync();

                foreach (var schedule in schedules)
                {
                    schedule.IsAvailable = newAvailability;
                }

                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = $"Successfully updated {schedules.Count} schedule(s) to {newAvailability}." });

                TempData["SuccessMessage"] = $"Successfully updated {schedules.Count} schedule(s) to {newAvailability}.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating schedules: {ex.Message}");

                if (IsAjaxRequest())
                    return Json(new { success = false, error = "An error occurred while updating schedules." });

                TempData["ErrorMessage"] = "An error occurred while updating schedules.";
            }

            return RedirectToAction(nameof(Availability));
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}