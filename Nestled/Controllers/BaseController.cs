using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Nestled.Data;
using System.Threading.Tasks;

namespace Nestled.Controllers
{
    public class BaseController : Controller, IAsyncActionFilter
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;

        public BaseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var settings = await _context.UserAppSettings
                        .FirstOrDefaultAsync(s => s.UserId == user.Id);
                    ViewData["ThemeMode"] = settings?.ThemeMode ?? "system";
                    ViewData["FontSize"] = settings?.FontSize ?? "medium";
                }
                else
                {
                    ViewData["ThemeMode"] = "system";
                    ViewData["FontSize"] = "medium";
                }
            }
            else
            {
                ViewData["ThemeMode"] = "system";
                ViewData["FontSize"] = "medium";
            }

            await next(); // continue to the next filter/action
        }

        protected bool IsPinConfirmedRecently()
        {
            var lastConfirmed = HttpContext.Session.GetString("VaultPinConfirmed");
            return lastConfirmed != null &&
                   DateTime.TryParse(lastConfirmed, out var confirmedTime) &&
                   confirmedTime > DateTime.UtcNow.AddMinutes(-5);
        }
    }
}
