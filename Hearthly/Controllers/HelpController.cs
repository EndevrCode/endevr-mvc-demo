using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Hearthly.Data;

namespace Hearthly.Controllers
{
    [Authorize]
    public class HelpController : BaseController
    {
        public HelpController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager) { }

        public IActionResult Index() => View();
    }
}
