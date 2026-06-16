using Hearthly.Data;
using Hearthly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers;

[Authorize]
public class StaffController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public StaffController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    private async Task<List<int>> GetUserFamilyIds(string userId) =>
        await _context.FamilyMembers.Where(m => m.UserId == userId).Select(m => m.FamilyId).ToListAsync();

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var staff = await _context.Staff.Include(s => s.Family).Where(s => ids.Contains(s.FamilyId)).ToListAsync();
        return View(staff);
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
    public async Task<IActionResult> Create(Staff staff, IFormFile? photo)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(staff.FamilyId)) return Forbid();

        if (photo != null && photo.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "uploads", "staff");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await photo.CopyToAsync(stream);
            staff.PhotoPath = $"/uploads/staff/{fileName}";
        }

        ModelState.Remove("Family");
        if (!ModelState.IsValid)
        {
            ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
            return View(staff);
        }

        _context.Staff.Add(staff);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null || !ids.Contains(staff.FamilyId)) return NotFound();
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View(staff);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Staff updated, IFormFile? photo)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null || !ids.Contains(staff.FamilyId)) return NotFound();

        staff.FirstName = updated.FirstName;
        staff.LastName = updated.LastName;
        staff.PhoneNumber = updated.PhoneNumber;
        staff.Email = updated.Email;
        staff.Nationality = updated.Nationality;
        staff.IdNumber = updated.IdNumber;
        staff.PassportNumber = updated.PassportNumber;
        staff.WorkDays = updated.WorkDays;
        staff.DailyWage = updated.DailyWage;
        staff.BankName = updated.BankName;
        staff.BankAccountNumber = updated.BankAccountNumber;
        staff.BankAccountType = updated.BankAccountType;
        staff.Address = updated.Address;
        staff.Notes = updated.Notes;

        if (photo != null && photo.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "uploads", "staff");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await photo.CopyToAsync(stream);
            staff.PhotoPath = $"/uploads/staff/{fileName}";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var staff = await _context.Staff.FindAsync(id);
        if (staff == null || !ids.Contains(staff.FamilyId)) return NotFound();
        _context.Staff.Remove(staff);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
