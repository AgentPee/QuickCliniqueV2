using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuickClinique.Models;

namespace QuickClinique.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Message
        public async Task<IActionResult> Index()
        {
            var messages = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver);
            var result = await messages.ToListAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, data = result });

            return View(result);
        }

        // GET: Message/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Message not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = message });

            return View(message);
        }

        // GET: Message/Create
        public IActionResult Create()
        {
            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name");
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name");

            if (IsAjaxRequest())
                return Json(new
                {
                    success = true,
                    senders = ViewData["SenderId"],
                    receivers = ViewData["ReceiverId"]
                });

            return View();
        }

        // POST: Message/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SenderId,ReceiverId,Message1,CreatedAt")] Message message)
        {
            if (ModelState.IsValid)
            {
                _context.Add(message);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Message created successfully", id = message.MessageId });

                return RedirectToAction(nameof(Index));
            }

            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.SenderId);
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.ReceiverId);

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

            return View(message);
        }

        // GET: Message/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Message not found" });
                return NotFound();
            }

            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.SenderId);
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.ReceiverId);

            if (IsAjaxRequest())
                return Json(new { success = true, data = message });

            return View(message);
        }

        // POST: Message/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MessageId,SenderId,ReceiverId,Message1,CreatedAt")] Message message)
        {
            if (id != message.MessageId)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID mismatch" });
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();

                    if (IsAjaxRequest())
                        return Json(new { success = true, message = "Message updated successfully" });

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MessageExists(message.MessageId))
                    {
                        if (IsAjaxRequest())
                            return Json(new { success = false, error = "Message not found" });
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.SenderId);
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.ReceiverId);

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

            return View(message);
        }

        // GET: Message/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "ID not provided" });
                return NotFound();
            }

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Message not found" });
                return NotFound();
            }

            if (IsAjaxRequest())
                return Json(new { success = true, data = message });

            return View(message);
        }

        // POST: Message/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                if (IsAjaxRequest())
                    return Json(new { success = true, message = "Message deleted successfully" });
            }
            else
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, error = "Message not found" });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MessageExists(int id)
        {
            return _context.Messages.Any(e => e.MessageId == id);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.ContentType == "application/json";
        }
    }
}