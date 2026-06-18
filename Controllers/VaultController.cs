using Nestled.Data;
using Nestled.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nestled.Controllers;

[Authorize]
public class VaultController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private const string VaultUnlockKey = "VaultUnlockedAt";

    public VaultController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private bool IsVaultUnlocked()
    {
        var unlockedAt = HttpContext.Session.GetString(VaultUnlockKey);
        if (string.IsNullOrEmpty(unlockedAt)) return false;
        if (DateTime.TryParse(unlockedAt, out var dt))
            return (DateTime.UtcNow - dt).TotalMinutes < 10;
        return false;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (string.IsNullOrEmpty(user?.VaultPin))
            return RedirectToAction("SetPin");

        if (!IsVaultUnlocked())
            return View("PinEntry");

        return View("Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string pin)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        if (string.IsNullOrEmpty(user.VaultPin))
            return RedirectToAction("SetPin");

        // Compare hashed PIN
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        var result = hasher.VerifyHashedPassword(user, user.VaultPin, pin);

        if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success
            || result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.SuccessRehashNeeded)
        {
            HttpContext.Session.SetString(VaultUnlockKey, DateTime.UtcNow.ToString("O"));
            return RedirectToAction("Index");
        }

        TempData["Error"] = "Incorrect PIN";
        return View("PinEntry");
    }

    [HttpGet]
    public IActionResult SetPin() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPin(string pin, string confirmPin)
    {
        if (pin != confirmPin || pin.Length < 4)
        {
            TempData["Error"] = pin.Length < 4 ? "PIN must be at least 4 digits." : "PINs do not match.";
            return View();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Forbid();

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
        user.VaultPin = hasher.HashPassword(user, pin);
        await _userManager.UpdateAsync(user);
        HttpContext.Session.SetString(VaultUnlockKey, DateTime.UtcNow.ToString("O"));
        return RedirectToAction("Index");
    }

    public IActionResult Lock()
    {
        HttpContext.Session.Remove(VaultUnlockKey);
        return RedirectToAction("Index");
    }
}
