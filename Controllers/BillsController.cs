using Hearthly.Data;
using Hearthly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers;

[Authorize]
public class BillsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public BillsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
        var bills = await _context.Bills.Include(b => b.Family).Where(b => ids.Contains(b.FamilyId)).OrderBy(b => b.DueDate).ToListAsync();
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View(bills);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Bill bill)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(bill.FamilyId)) return Forbid();
        ModelState.Remove("Family");
        if (!ModelState.IsValid) { ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync(); return View(bill); }
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var bill = await _context.Bills.FindAsync(id);
        if (bill == null || !ids.Contains(bill.FamilyId)) return NotFound();
        bill.IsPaid = true;
        bill.PaidDate = DateOnly.FromDateTime(DateTime.Today);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var bill = await _context.Bills.FindAsync(id);
        if (bill == null || !ids.Contains(bill.FamilyId)) return NotFound();
        _context.Bills.Remove(bill);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
