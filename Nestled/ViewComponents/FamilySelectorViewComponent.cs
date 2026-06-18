using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nestled.Data;
using Nestled.Models;

namespace Nestled.ViewComponents
{
    public class FamilySelectorViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public FamilySelectorViewComponent(
            ApplicationDbContext db,
            UserManager<ApplicationUser> users)
        {
            _db = db;
            _users = users;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid? selectedFamilyId)
        {
            // 1) Get the current user's ID
            var userId = _users.GetUserId(HttpContext.User);

            // 2) Load all families I'm in and accepted
            var families = await _db.FamilyMembers
                .Where(m => m.UserId == userId && m.IsAccepted)
                .Select(m => m.Family)
                .OrderBy(f => f.Name)
                .ToListAsync();

            // 3) Build the view model
            var vm = new FamilySelectorViewModel
            {
                Families = families,
                SelectedFamilyId = selectedFamilyId
            };

            // 4) Render the Default view (we’ll create that in Step 3)
            return View(vm);
        }
    }
}
