using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Models;

namespace Hearthly.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
            : base(context, userManager)
        {
            _env = env;
            _signInManager = signInManager;
        }

// GET: /Profile/Details/{id?}
// if id is null, shows your own profile
[HttpGet]
        public async Task<IActionResult> Details(string? id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = string.IsNullOrEmpty(id) ? currentUserId : id;

            if (userId == null)
                return Challenge();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var profile = await _context.UserProfiles
                                 .FirstOrDefaultAsync(p => p.UserId == userId)
                          ?? new UserProfile { UserId = userId };

            var vm = new ProfileViewModel
            {
                User = user,
                Profile = profile
            };

            return View(vm);
        }

        // GET: /Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? throw new InvalidOperationException("Not signed in");

            var profile = await _context.UserProfiles.FindAsync(userId)
                          ?? new UserProfile { UserId = userId };

            return View(profile);
        }

        // POST: /Profile/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfile profile, IFormFile? photo)
        {
            // Always use the authenticated user's ID — never trust the submitted value
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Challenge();
            profile.UserId = currentUserId;

            ModelState.Remove(nameof(profile.User));
            ModelState.Remove(nameof(profile.PhotoPath));

            if (!ModelState.IsValid)
                return View(profile);

            if (photo is { Length: > 0 })
            {
                var uploads = Path.Combine(_env.WebRootPath, "images", "profiles");
                Directory.CreateDirectory(uploads);

                var ext = Path.GetExtension(photo.FileName);
                var fileName = profile.UserId + ext;
                var path = Path.Combine(uploads, fileName);

                await using var fs = System.IO.File.Create(path);
                await photo.CopyToAsync(fs);

                profile.PhotoPath = $"/images/profiles/{fileName}";
            }
            else
            {
                // Retain existing photo if not uploading a new one
                var existing = await _context.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == profile.UserId);
                if (existing != null)
                    profile.PhotoPath = existing.PhotoPath;
            }

            if (await _context.UserProfiles.AnyAsync(p => p.UserId == profile.UserId))
                _context.UserProfiles.Update(profile);
            else
                _context.UserProfiles.Add(profile);

            if (string.IsNullOrWhiteSpace(profile.PhotoPath))
            {
                profile.PhotoPath = "/images/default-profile.png"; // fallback to default
            }
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Your profile has been saved.";
            return RedirectToAction(nameof(Details));
        }

        // NEW: POST /Profile/DeleteAccount
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Error deleting your account.";
                return RedirectToAction(nameof(Details));
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
