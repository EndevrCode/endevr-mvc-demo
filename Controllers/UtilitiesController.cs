using Hearthly.Data;
using Hearthly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers;

[Authorize]
public class UtilitiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UtilitiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private async Task<List<int>> GetUserFamilyIds(string userId) =>
        await _context.FamilyMembers.Where(m => m.UserId == userId).Select(m => m.FamilyId).ToListAsync();

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var accounts = await _context.UtilityAccounts
            .Include(a => a.Purchases.OrderByDescending(p => p.PurchaseDate).Take(5))
            .Include(a => a.Family)
            .Where(a => ids.Contains(a.FamilyId))
            .ToListAsync();
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View(accounts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(UtilityAccount account)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(account.FamilyId)) return Forbid();
        ModelState.Remove("Family");
        ModelState.Remove("Purchases");
        if (!ModelState.IsValid) return RedirectToAction("Index");
        _context.UtilityAccounts.Add(account);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPurchase(UtilityPurchase purchase)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var account = await _context.UtilityAccounts.FindAsync(purchase.UtilityAccountId);
        if (account == null || !ids.Contains(account.FamilyId)) return Forbid();
        ModelState.Remove("UtilityAccount");
        if (!ModelState.IsValid) return RedirectToAction("Index");
        purchase.PurchaseDate = DateTime.UtcNow;
        _context.UtilityPurchases.Add(purchase);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
