using Nestled.Data;
using Nestled.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nestled.Controllers;

[Authorize]
public class BankingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private const string VaultUnlockKey = "VaultUnlockedAt";

    public BankingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    private bool IsVaultUnlocked()
    {
        var unlockedAt = HttpContext.Session.GetString(VaultUnlockKey);
        if (string.IsNullOrEmpty(unlockedAt)) return false;
        if (DateTime.TryParse(unlockedAt, out var dt))
            return (DateTime.UtcNow - dt).TotalMinutes < 10;
        return false;
    }

    private async Task<List<int>> GetUserFamilyIds(string userId) =>
        await _context.FamilyMembers.Where(m => m.UserId == userId).Select(m => m.FamilyId).ToListAsync();

    public async Task<IActionResult> Index()
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var accounts = await _context.BankAccounts.Where(a => ids.Contains(a.FamilyId)).ToListAsync();
        return View(accounts);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BankAccount account, IFormFile? cardFront, IFormFile? cardBack)
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(account.FamilyId)) return Forbid();

        async Task<string?> SaveCard(IFormFile? file, string name)
        {
            if (file == null || file.Length == 0) return null;
            var dir = Path.Combine(_env.WebRootPath, "uploads", "cards");
            Directory.CreateDirectory(dir);
            var fn = $"{name}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            using var s = new FileStream(Path.Combine(dir, fn), FileMode.Create);
            await file.CopyToAsync(s);
            return $"/uploads/cards/{fn}";
        }

        account.CardFrontImagePath = await SaveCard(cardFront, "front");
        account.CardBackImagePath = await SaveCard(cardBack, "back");
        ModelState.Remove("Family");
        _context.BankAccounts.Add(account);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var account = await _context.BankAccounts.FindAsync(id);
        if (account == null || !ids.Contains(account.FamilyId)) return NotFound();
        _context.BankAccounts.Remove(account);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
