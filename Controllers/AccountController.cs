using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace safevault_application.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AccountController(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    [HttpGet("Login")]
    public IActionResult Login(string returnUrl = "/")
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet("Register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("Register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPost(string username, string password, string confirmPassword, string role = "User")
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] = "Username and password are required.";
            return RedirectToAction(nameof(Register));
        }

        if (password != confirmPassword)
        {
            TempData["ErrorMessage"] = "Passwords do not match.";
            return RedirectToAction(nameof(Register));
        }

        // Only allow assigning Admin role if current user is an Admin
        var assignedRole = "User";
        if (User?.Identity?.IsAuthenticated == true && User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(role))
        {
            assignedRole = role;
        }

        // ensure role exists
        if (!await _roleManager.RoleExistsAsync(assignedRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(assignedRole));
        }

        var user = new IdentityUser { UserName = username, Email = username + "@local" };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Register));
        }

        await _userManager.AddToRoleAsync(user, assignedRole);
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(string username, string password, string returnUrl = "/")
    {
        var result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Invalid username or password.";
            return RedirectToAction(nameof(Login));
        }

        return Redirect(returnUrl);
    }

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet("AccessDenied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
