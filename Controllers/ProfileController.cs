using Nestled.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Nestled.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string firstName, string lastName, string? preferredName,
        string? birthDate, string? idNumber, string? phoneNumber, string? address,
        ThemeMode themeMode, FontSize fontSize, IFormFile? photo)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PreferredName = preferredName;
        user.IdNumber = idNumber;
        user.PhoneNumber = phoneNumber;
        user.Address = address;
        user.ThemeMode = themeMode;
        user.FontSize = fontSize;

        if (!string.IsNullOrEmpty(birthDate) && DateOnly.TryParse(birthDate, out var parsedDate))
            user.BirthDate = parsedDate;

        if (photo != null && photo.Length > 0)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{user.Id}_{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);
            user.PhotoPath = $"/uploads/profiles/{fileName}";
        }

        await _userManager.UpdateAsync(user);
        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction("Index");
    }
}
