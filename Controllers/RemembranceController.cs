using Nestled.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nestled.Models;

namespace Nestled.Controllers;

[Authorize]
public class RemembranceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public RemembranceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await _context.FamilyMembers.Where(m => m.UserId == userId).Select(m => m.FamilyId).ToListAsync();
        var deceasedPets = await _context.Pets
            .Include(p => p.Family)
            .Where(p => ids.Contains(p.FamilyId) && p.IsDeceased)
            .OrderByDescending(p => p.DeceasedDate)
            .ToListAsync();
        return View(deceasedPets);
    }
}
