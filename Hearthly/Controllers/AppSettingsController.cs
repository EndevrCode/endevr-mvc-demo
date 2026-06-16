using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.ViewModels;

namespace Hearthly.Controllers
{
    [Authorize]
    public class AppSettingsController : BaseController
    {
        public AppSettingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
     : base(context, userManager)
        {
        }

        // GET: /AppSettings
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var settings = await _context.UserAppSettings
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            var viewModel = new AppSettingsViewModel
            {
                ThemeMode = settings?.ThemeMode ?? "system",
                FontSize = settings?.FontSize ?? "medium"
            };

            return View(viewModel);
        }

        // POST: /AppSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AppSettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            var settings = await _context.UserAppSettings
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (settings == null)
            {
                settings = new UserAppSettings
                {
                    UserId = user.Id,
                    ThemeMode = model.ThemeMode,
                    FontSize = model.FontSize
                };
                _context.UserAppSettings.Add(settings);
            }
            else
            {
                settings.ThemeMode = model.ThemeMode;
                settings.FontSize = model.FontSize;
                _context.UserAppSettings.Update(settings);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Settings saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
