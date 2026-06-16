using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;

namespace Hearthly.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            Guid firstFamilyId = Guid.Empty;
            string preferredName = "there";
            bool isInFamily = false;

            if (user != null)
            {
                // Preferred name
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == user.Id);
                preferredName = profile?.PreferredName ?? profile?.FirstName ?? "there";

                // First accepted family
                firstFamilyId = await _context.FamilyMembers
                    .Where(m => m.UserId == user.Id && m.IsAccepted)
                    .OrderBy(m => m.JoinedAt)
                    .Select(m => m.FamilyId)
                    .FirstOrDefaultAsync();

                // Check if the user is in any family
                isInFamily = await _context.FamilyMembers
                    .AnyAsync(m => m.UserId == user.Id && m.IsAccepted);
            }

            ViewData["PreferredName"] = preferredName;
            ViewData["FirstFamilyId"] = firstFamilyId;
            ViewData["IsInFamily"] = isInFamily;

            return View();
        }

        public IActionResult ComingSoon()
        {
            return View("~/Views/Shared/ComingSoon.cshtml");
        }


        // Other actions (Privacy, Error, etc.) ...
    }
}
