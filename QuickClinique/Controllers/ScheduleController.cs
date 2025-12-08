using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;
using QuickClinique.Services;

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
            // Check if date is Sunday - scheduling is disabled on Sundays
            if (schedule.Date.DayOfWeek == DayOfWeek.Sunday)
            {
                var errorMessage = "Sunday scheduling is disabled. Please select a different date.";
                
                if (IsAjaxRequest())
                    return Json(new { success = false, error = errorMessage });
                
                ModelState.AddModelError("Date", errorMessage);
            }
            
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
                    // If EndDate is not provided or equals StartDate, create for single day only
                    var endDate = model.EndDate ?? model.StartDate;
                    
                    // Calculate how many schedules will be created (before generating them)
                    int estimatedCount = 0;
                    var currentDateForEstimate = model.StartDate;
                    while (currentDateForEstimate <= endDate)
                    {
                        if (ShouldCreateForDay(currentDateForEstimate, model.SelectedDays))
                        {
                            // Calculate number of time slots (15-minute intervals)
                            var timeDiff = (model.EndTime - model.StartTime).TotalMinutes;
                            estimatedCount += (int)Math.Ceiling(timeDiff / 15);
                        }
                        currentDateForEstimate = currentDateForEstimate.AddDays(1);
                    }

                    // Maximum limit to prevent OutOfMemoryException (e.g., 1000 schedules per request)
                    const int MAX_SCHEDULES_PER_REQUEST = 1000;
                    if (estimatedCount > MAX_SCHEDULES_PER_REQUEST)
                    {
                        var errorMessage = $"Too many schedules to create at once ({estimatedCount}). Maximum allowed is {MAX_SCHEDULES_PER_REQUEST}. Please reduce the date range or create schedules in smaller batches.";
                        
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = errorMessage });

                        ModelState.AddModelError("", errorMessage);
                        return View(model);
                    }

                    // Process schedules in batches to avoid memory issues
                    const int BATCH_SIZE = 500;
                    int totalCreated = 0;
                    var currentDate = model.StartDate;
                    var batch = new List<Schedule>();

                    // Generate and save schedules in batches
                    while (currentDate <= endDate)
                    {
                        // Check if we should create schedule for this day of week
                        if (ShouldCreateForDay(currentDate, model.SelectedDays))
                        {
                            // Generate 15-minute time slots from StartTime to EndTime
                            var currentTime = model.StartTime;
                            while (currentTime < model.EndTime)
                            {
                                var slotEndTime = currentTime.AddMinutes(15);
                                // Ensure we don't exceed the end time
                                if (slotEndTime > model.EndTime)
                                {
                                    slotEndTime = model.EndTime;
                                }

                                var schedule = new Schedule
                                {
                                    Date = currentDate,
                                    StartTime = currentTime,
                                    EndTime = slotEndTime,
                                    IsAvailable = model.IsAvailable
                                };
                                batch.Add(schedule);

                                // If batch is full, save it and clear
                                if (batch.Count >= BATCH_SIZE)
                                {
                                    _context.Schedules.AddRange(batch);
                                    await _context.SaveChangesAsync().ConfigureAwait(false);
                                    totalCreated += batch.Count;
                                    batch.Clear();
                                }

                                // Move to next 15-minute slot
                                currentTime = currentTime.AddMinutes(15);
                            }
                        }
                        currentDate = currentDate.AddDays(1);
                    }

                    // Save any remaining schedules in the batch
                    if (batch.Count > 0)
                    {
                        _context.Schedules.AddRange(batch);
                        await _context.SaveChangesAsync().ConfigureAwait(false);
                        totalCreated += batch.Count;
                    }

                    if (totalCreated > 0)
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = true, message = $"Successfully created {totalCreated} schedule(s)!" });

                        TempData["SuccessMessage"] = $"Successfully created {totalCreated} schedule(s)!";
                        return RedirectToAction(nameof(Availability));
                    }
                    else
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "No schedules were created. Please check your date range and selected days." });

                        ModelState.AddModelError("", "No schedules were created. Please check your date range and selected days.");
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    Console.WriteLine($"OutOfMemoryException creating schedules: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    var errorMessage = "Too many schedules to create at once. The server ran out of memory. Please reduce the date range and create schedules in smaller batches (e.g., 1-2 weeks at a time).";

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = errorMessage });

                    ModelState.AddModelError("", errorMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating multiple schedules: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");

                    var errorMessage = "An error occurred while creating schedules. Please try again with a smaller date range.";

                    if (IsAjaxRequest())
                        return Json(new { success = false, error = errorMessage });

                    ModelState.AddModelError("", errorMessage);
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
                // Check if any Sunday dates are selected
                var sundayDates = model.SelectedDates.Where(d => d.DayOfWeek == DayOfWeek.Sunday).ToList();
                if (sundayDates.Any())
                {
                    var errorMessage = $"Sunday scheduling is disabled. Please remove the following date(s): {string.Join(", ", sundayDates.Select(d => d.ToString("yyyy-MM-dd")))}";
                    
                    if (IsAjaxRequest())
                        return Json(new { success = false, error = errorMessage });
                    
                    ModelState.AddModelError("SelectedDates", errorMessage);
                }
                
                // Only proceed if there are no Sunday dates
                if (!sundayDates.Any())
                {
                    try
                    {
                        var schedules = new List<Schedule>();

                        foreach (var date in model.SelectedDates)
                        {
                            // Double-check: skip Sunday dates
                            if (date.DayOfWeek == DayOfWeek.Sunday)
                                continue;
                                
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
            // Always exclude Sunday - scheduling is disabled on Sundays
            if (date.DayOfWeek == DayOfWeek.Sunday)
                return false;

            // If no days are selected, create for all days (except Sunday)
            if (selectedDays == null || !selectedDays.Any())
                return true;

            // If days are selected, exclude those days (create for all other days)
            var dayOfWeek = date.DayOfWeek.ToString();
            return !selectedDays.Contains(dayOfWeek);
        }

        // GET: Schedule/Availability
        public async Task<IActionResult> Availability(string status = "All", DateOnly? startDate = null, DateOnly? endDate = null, bool showPast = false)
        {
            ViewData["CurrentFilter"] = status;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");
            ViewData["ShowPast"] = showPast;

            // Use Philippine Time (UTC+8) for consistent timezone handling across environments
            var today = TimeZoneHelper.GetPhilippineDate();
            var currentTime = TimeZoneHelper.GetPhilippineTimeOnly();

            var schedules = _context.Schedules.AsQueryable();

            // Filter by availability status
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                schedules = schedules.Where(s => s.IsAvailable == status);
            }

            // Filter out past schedules by default (unless showPast is true)
            if (!showPast)
            {
                schedules = schedules.Where(s => 
                    s.Date > today || 
                    (s.Date == today && s.EndTime > currentTime)
                );
            }

            // Filter by date range (only if not filtering past schedules)
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

            var model = await schedules.ToListAsync().ConfigureAwait(false);

            // Clear change tracker to ensure clean state for statistics queries
            // This prevents "concurrent operation" errors when executing multiple queries
            _context.ChangeTracker.Clear();

            // Calculate statistics sequentially - EF Core doesn't support concurrent operations on same DbContext
            // Must await each query completely before starting the next one
            // These queries are now optimized with indexes, so sequential execution should be fast
            ViewBag.TotalSchedules = await _context.Schedules.CountAsync().ConfigureAwait(false);
            ViewBag.AvailableSchedules = await _context.Schedules.CountAsync(s => s.IsAvailable == "Yes").ConfigureAwait(false);
            ViewBag.UnavailableSchedules = await _context.Schedules.CountAsync(s => s.IsAvailable == "No").ConfigureAwait(false);
            ViewBag.PastSchedules = await _context.Schedules.CountAsync(s => 
                s.Date < today || 
                (s.Date == today && s.EndTime < currentTime)
            ).ConfigureAwait(false);

            if (IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    data = model,
                    statistics = new
                    {
                        total = ViewBag.TotalSchedules,
                        available = ViewBag.AvailableSchedules,
                        unavailable = ViewBag.UnavailableSchedules,
                        past = ViewBag.PastSchedules
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

        // POST: Schedule/BulkDelete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(int[] scheduleIds)
        {
            if (scheduleIds == null || !scheduleIds.Any())
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "No schedules selected for deletion." });

                TempData["ErrorMessage"] = "No schedules selected for deletion.";
                return RedirectToAction(nameof(Availability));
            }

            try
            {
                var schedules = await _context.Schedules
                    .Include(s => s.Appointments)
                    .Where(s => scheduleIds.Contains(s.ScheduleId))
                    .ToListAsync();

                // Get appointment count before deletion
                var totalAppointments = schedules.Sum(s => s.Appointments?.Count ?? 0);

                // Remove all schedules
                _context.Schedules.RemoveRange(schedules);
                await _context.SaveChangesAsync();

                var message = totalAppointments > 0
                    ? $"Successfully deleted {schedules.Count} schedule(s) and {totalAppointments} associated appointment(s)."
                    : $"Successfully deleted {schedules.Count} schedule(s).";

                if (IsAjaxRequest())
                    return Json(new { success = true, message = message });

                TempData["SuccessMessage"] = message;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting schedules: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (IsAjaxRequest())
                    return Json(new { success = false, error = "An error occurred while deleting schedules." });

                TempData["ErrorMessage"] = "An error occurred while deleting schedules.";
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