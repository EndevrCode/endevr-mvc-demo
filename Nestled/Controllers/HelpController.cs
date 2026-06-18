using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nestled.Data;

namespace Nestled.Controllers
{
    [Authorize]
    public class HelpController : BaseController
    {
        public HelpController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager) { }

        public IActionResult Index() => View();
    }
}
