using Hearthly.Data;
using Hearthly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers;

[Authorize]
public class ServiceContactsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ServiceContactsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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
        var contacts = await _context.ServiceContacts.Where(c => ids.Contains(c.FamilyId)).ToListAsync();
        return View(contacts);
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
    public async Task<IActionResult> Create(ServiceContact contact)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(contact.FamilyId)) return Forbid();
        ModelState.Remove("Family");
        if (!ModelState.IsValid) { ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync(); return View(contact); }
        _context.ServiceContacts.Add(contact);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var contact = await _context.ServiceContacts.FindAsync(id);
        if (contact == null || !ids.Contains(contact.FamilyId)) return NotFound();
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View(contact);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceContact updated)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var contact = await _context.ServiceContacts.FindAsync(id);
        if (contact == null || !ids.Contains(contact.FamilyId)) return NotFound();
        contact.Category = updated.Category;
        contact.Name = updated.Name;
        contact.Phone = updated.Phone;
        contact.Email = updated.Email;
        contact.Address = updated.Address;
        contact.Notes = updated.Notes;
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var contact = await _context.ServiceContacts.FindAsync(id);
        if (contact == null || !ids.Contains(contact.FamilyId)) return NotFound();
        _context.ServiceContacts.Remove(contact);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
