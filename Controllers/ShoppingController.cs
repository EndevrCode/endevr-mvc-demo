using Nestled.Data;
using Nestled.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nestled.Controllers;

[Authorize]
public class ShoppingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShoppingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
        var lists = await _context.ShoppingLists
            .Include(l => l.Items)
            .Where(l => ids.Contains(l.FamilyId) && !l.IsArchived)
            .ToListAsync();
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View(lists);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateList(string name, int familyId)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(familyId)) return Forbid();
        _context.ShoppingLists.Add(new ShoppingList { Name = name, FamilyId = familyId });
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int listId, string name, decimal quantity, string? unit)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var list = await _context.ShoppingLists.FindAsync(listId);
        if (list == null || !ids.Contains(list.FamilyId)) return Forbid();
        _context.ShoppingItems.Add(new ShoppingItem { ShoppingListId = listId, Name = name, Quantity = quantity, Unit = unit });
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleItem(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var item = await _context.ShoppingItems.Include(i => i.ShoppingList).FirstOrDefaultAsync(i => i.Id == id);
        if (item?.ShoppingList == null || !ids.Contains(item.ShoppingList.FamilyId)) return Forbid();
        item.IsChecked = !item.IsChecked;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteList(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var list = await _context.ShoppingLists.FindAsync(id);
        if (list == null || !ids.Contains(list.FamilyId)) return NotFound();
        _context.ShoppingLists.Remove(list);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
