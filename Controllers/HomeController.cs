using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using safevault_application.Models;

namespace safevault_application.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("/submit")]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(string username, string email)
    {
        // The Home form previously saved user details to a legacy store.
        // Now we direct users to the Identity registration flow instead.
        return RedirectToAction("Register", "Account");
    }

    public IActionResult Welcome()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Admin()
    {
        return View();
    }

    [HttpGet("/api/secure-data")]
    [Authorize]
    public IActionResult SecureData()
    {
        return Ok(new { message = "Authenticated data" });
    }

    [HttpGet("/api/admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "Admin data" });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
