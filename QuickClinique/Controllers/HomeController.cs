using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuickClinique.Models;
using QuickClinique.Attributes;

namespace QuickClinique.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
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

    private bool IsAjaxRequest()
    {
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               Request.ContentType == "application/json";
    }
}