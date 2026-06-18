using Nestled.Data;
using Nestled.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nestled.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var userFamilyIds = await _context.FamilyMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.FamilyId)
            .ToListAsync();

        var families = await _context.Families
            .Include(f => f.Members).ThenInclude(m => m.User)
            .Where(f => userFamilyIds.Contains(f.Id))
            .ToListAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var in30Days = today.AddDays(30);

        // Upcoming birthdays from family members
        var allMembers = families.SelectMany(f => f.Members)
            .Where(m => m.User?.BirthDate.HasValue == true)
            .Select(m => m.User!)
            .Distinct()
            .ToList();

        var upcomingBirthdays = allMembers
            .Where(u =>
            {
                if (!u.BirthDate.HasValue) return false;
                var bday = u.BirthDate.Value;
                var thisYearBday = new DateOnly(today.Year, bday.Month, bday.Day);
                if (thisYearBday < today) thisYearBday = thisYearBday.AddYears(1);
                return thisYearBday <= in30Days;
            })
            .OrderBy(u =>
            {
                var bday = u.BirthDate!.Value;
                var thisYearBday = new DateOnly(today.Year, bday.Month, bday.Day);
                if (thisYearBday < today) thisYearBday = thisYearBday.AddYears(1);
                return thisYearBday;
            })
            .Take(5)
            .ToList();

        var pendingInvites = await _context.FamilyInvitations
            .Include(i => i.Family)
            .Where(i => userFamilyIds.Contains(i.FamilyId) && i.Status == InvitationStatus.Pending)
            .ToListAsync();

        var unpaidBills = await _context.Bills
            .Where(b => userFamilyIds.Contains(b.FamilyId) && !b.IsPaid)
            .OrderBy(b => b.DueDate)
            .Take(5)
            .ToListAsync();

        var petCareReminders = await _context.PetCareRecords
            .Include(r => r.Pet)
            .Where(r => userFamilyIds.Contains(r.Pet!.FamilyId)
                        && r.NextDueDate.HasValue
                        && r.NextDueDate.Value <= in30Days)
            .OrderBy(r => r.NextDueDate)
            .Take(5)
            .ToListAsync();

        ViewBag.Families = families;
        ViewBag.UpcomingBirthdays = upcomingBirthdays;
        ViewBag.PendingInvites = pendingInvites;
        ViewBag.UnpaidBills = unpaidBills;
        ViewBag.PetCareReminders = petCareReminders;

        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }
}
