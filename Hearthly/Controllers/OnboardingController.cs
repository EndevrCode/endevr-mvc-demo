using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using System.Threading.Tasks;

namespace Hearthly.Controllers
{
    [Authorize]
    public class OnboardingController : BaseController
    {
        public OnboardingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        // STEP 1: Redirect to Profile if not complete
        public async Task<IActionResult> Step1()
        {
            var userId = _userManager.GetUserId(User);

            var user = await _userManager.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Redirect to profile if key info is missing
            if (string.IsNullOrWhiteSpace(user.Profile?.PreferredName))
            {
                return RedirectToAction("Edit", "Profile");
            }

            // Proceed to next step
            return RedirectToAction("Step2");
        }
        // STEP 2: Redirect to Profile if not complete, then move to Dash board
        public async Task<IActionResult> Step2()
        {
            var user = await _userManager.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == _userManager.GetUserId(User));

            if (string.IsNullOrWhiteSpace(user?.Profile?.PreferredName))
                return RedirectToAction("Edit", "Profile");

            return View();
        }
        // Later steps will go here...
    }
}
