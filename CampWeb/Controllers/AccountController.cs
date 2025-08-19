using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CampWeb.Models;

namespace CampWeb.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string returnUrl = "/")
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Login attempt with missing email or password");
            return LocalRedirect($"/prihlaseni?error={Uri.EscapeDataString("Email a heslo jsou povinné")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully", email);
            return LocalRedirect(returnUrl);
        }

        string errorMessage;
        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} account locked out", email);
            errorMessage = "Účet je zablokován";
        }
        else if (result.RequiresTwoFactor)
        {
            _logger.LogInformation("User {Email} requires two factor authentication", email);
            errorMessage = "Vyžaduje se dvoufaktorové ověření";
        }
        else
        {
            _logger.LogWarning("Failed login attempt for {Email}", email);
            errorMessage = "Nesprávné přihlašovací údaje";
        }

        return LocalRedirect($"/prihlaseni?error={Uri.EscapeDataString(errorMessage)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return LocalRedirect("/");
    }
}
