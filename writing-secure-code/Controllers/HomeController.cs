using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using writing_secure_code.Helpers;
using writing_secure_code.Models;

namespace writing_secure_code.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly LoginHelper _loginHelper;

    public HomeController(ILogger<HomeController> logger, LoginHelper loginHelper)
    {
        _logger = logger;
        _loginHelper = loginHelper;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("/submit")]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(string username, string email)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Please enter both a username and an email address.";
            return RedirectToAction(nameof(Index));
        }

        var saved = _loginHelper.SaveUser(username, email);
        if (!saved)
        {
            TempData["ErrorMessage"] = "We could not save your details. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData["WelcomeName"] = username;
        return RedirectToAction(nameof(Welcome));
    }

    public IActionResult Welcome()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
