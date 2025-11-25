using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QuickClinique.Models;
using QuickClinique.Attributes;
using QuickClinique.Hubs;
using Microsoft.EntityFrameworkCore;

namespace QuickClinique.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<MessageHub> _hubContext;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IHubContext<MessageHub> hubContext)
    {
        _logger = logger;
        _context = context;
        _hubContext = hubContext;
    }

    [StudentOnly]
    public IActionResult Index()
    {
        if (IsAjaxRequest())
            return Json(new { success = true, message = "Welcome to QuickClinique" });

        return View();
    }

    public IActionResult Privacy()
    {
        if (IsAjaxRequest())
            return Json(new { success = true, message = "Privacy Policy" });

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var errorModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };

        if (IsAjaxRequest())
            return Json(new { success = false, error = "An error occurred", requestId = errorModel.RequestId });

        return View(errorModel);
    }

    public IActionResult AccessDenied(string message)
    {
        ViewData["ErrorMessage"] = message ?? "You don't have permission to access this page.";

        if (IsAjaxRequest())
            return Json(new { success = false, error = ViewData["ErrorMessage"] });

        return View();
    }

    // GET: Home/GetMessages - Get messages for current student
    [HttpGet]
    public async Task<IActionResult> GetMessages()
    {
        var studentId = HttpContext.Session.GetInt32("StudentId");
        if (studentId == null)
        {
            return Json(new { success = false, error = "Not logged in" });
        }

        // Get student's userId from Students table
        var student = await _context.Students.FindAsync(studentId.Value);
        if (student == null)
        {
            return Json(new { success = false, error = "Student not found" });
        }

        // Get all messages where student is sender or receiver
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == student.UserId || m.ReceiverId == student.UserId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new {
                messageId = m.MessageId,
                senderId = m.SenderId,
                receiverId = m.ReceiverId,
                senderName = m.Sender.Name,
                receiverName = m.Receiver.Name,
                message = m.Message1,
                createdAt = m.CreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                isSent = m.SenderId == student.UserId
            })
            .ToListAsync();

        return Json(new { success = true, data = messages, currentUserId = student.UserId });
    }

    // POST: Home/SendMessage - Send a message to clinic staff
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var studentId = HttpContext.Session.GetInt32("StudentId");
        if (studentId == null)
        {
            return Json(new { success = false, error = "Not logged in" });
        }

        // Get student's userId with User navigation property
        var student = await _context.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
        
        if (student == null)
        {
            return Json(new { success = false, error = "Student not found" });
        }

        // Get a clinic staff member to send message to
        // Note: Message is sent to one staff member, but ALL clinic staff can view and reply (shared inbox)
        var clinicStaff = await _context.Clinicstaffs
            .Include(c => c.User)
            .FirstOrDefaultAsync();
        
        if (clinicStaff == null)
        {
            return Json(new { success = false, error = "No clinic staff available" });
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Json(new { success = false, error = "Message cannot be empty" });
        }

        var message = new Message
        {
            SenderId = student.UserId,
            ReceiverId = clinicStaff.UserId,
            Message1 = request.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Reload message with navigation properties for SignalR broadcast
        var savedMessage = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.MessageId == message.MessageId);

        var messageData = new
        {
            messageId = savedMessage.MessageId,
            senderId = savedMessage.SenderId,
            receiverId = savedMessage.ReceiverId,
            senderName = savedMessage.Sender.Name,
            receiverName = savedMessage.Receiver.Name,
            message = savedMessage.Message1,
            createdAt = savedMessage.CreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            isSent = true
        };

        // Broadcast to student (sender)
        await _hubContext.Clients.Group($"user_{student.UserId}").SendAsync("ReceiveMessage", messageData);

        // Broadcast to all clinic staff (shared inbox)
        await _hubContext.Clients.Group("clinic_staff").SendAsync("ReceiveMessage", new
        {
            messageId = savedMessage.MessageId,
            senderId = savedMessage.SenderId,
            receiverId = savedMessage.ReceiverId,
            senderName = savedMessage.Sender.Name,
            receiverName = savedMessage.Receiver.Name,
            message = savedMessage.Message1,
            createdAt = savedMessage.CreatedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            isSent = false // For clinic staff, this is a received message
        });

        return Json(new { 
            success = true, 
            message = "Message sent successfully",
            data = messageData
        });
    }

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               Request.ContentType == "application/json";
    }
}

public class SendMessageRequest
{
    public string Message { get; set; } = string.Empty;
}