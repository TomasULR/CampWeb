using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CampWeb.Models;

namespace CampWeb.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
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
            return LocalRedirect(
                $"/prihlaseni?error={Uri.EscapeDataString("Email a heslo jsou povinné")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        // Find user by email first
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("No user found with email {Email}", email);
            return LocalRedirect(
                $"/prihlaseni?error={Uri.EscapeDataString("Nesprávné přihlašovací údaje")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        // Sign in using the user object directly
        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully", email);
            
            // Optional: Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            
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

        return LocalRedirect(
            $"/prihlaseni?error={Uri.EscapeDataString(errorMessage)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
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