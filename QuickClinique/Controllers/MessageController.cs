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
            return View(await messages.ToListAsync());
        }

        // GET: Message/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
                return NotFound();

            return View(message);
        }

        // GET: Message/Create
        public IActionResult Create()
        {
            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name");
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name");
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.SenderId);
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.ReceiverId);
            return View(message);
        }

        // GET: Message/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound();

            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.SenderId);
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.ReceiverId);
            return View(message);
        }

        // POST: Message/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MessageId,SenderId,ReceiverId,Message1,CreatedAt")] Message message)
        {
            if (id != message.MessageId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MessageExists(message.MessageId))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["SenderId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.SenderId);
            ViewData["ReceiverId"] = new SelectList(_context.Usertypes, "UserId", "Name", message.ReceiverId);
            return View(message);
        }

        // GET: Message/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
                return NotFound();

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
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MessageExists(int id)
        {
            return _context.Messages.Any(e => e.MessageId == id);
        }
    }
}