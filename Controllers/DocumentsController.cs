using Hearthly.Data;
using Hearthly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers;

[Authorize]
public class DocumentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private const string VaultUnlockKey = "VaultUnlockedAt";

    public DocumentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    private bool IsVaultUnlocked()
    {
        var unlockedAt = HttpContext.Session.GetString(VaultUnlockKey);
        if (string.IsNullOrEmpty(unlockedAt)) return false;
        if (DateTime.TryParse(unlockedAt, out var dt)) return (DateTime.UtcNow - dt).TotalMinutes < 10;
        return false;
    }

    private async Task<List<int>> GetUserFamilyIds(string userId) =>
        await _context.FamilyMembers.Where(m => m.UserId == userId).Select(m => m.FamilyId).ToListAsync();

    public async Task<IActionResult> Index()
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var docs = await _context.VaultDocuments.Where(d => ids.Contains(d.FamilyId)).ToListAsync();
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View(docs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(VaultDocument doc, IFormFile file, int familyId)
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(familyId)) return Forbid();
        if (file == null || file.Length == 0) { TempData["Error"] = "Please select a file."; return RedirectToAction("Index"); }

        var dir = Path.Combine(_env.WebRootPath, "uploads", "documents");
        Directory.CreateDirectory(dir);
        var fn = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        using var stream = new FileStream(Path.Combine(dir, fn), FileMode.Create);
        await file.CopyToAsync(stream);

        doc.FamilyId = familyId;
        doc.UserId = userId;
        doc.FilePath = $"/uploads/documents/{fn}";
        doc.FileSize = file.Length;
        doc.CreatedAt = DateTime.UtcNow;
        ModelState.Remove("Family");
        ModelState.Remove("User");
        _context.VaultDocuments.Add(doc);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Download(int id)
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var doc = await _context.VaultDocuments.FindAsync(id);
        if (doc == null || !ids.Contains(doc.FamilyId)) return NotFound();
        var path = Path.Combine(_env.WebRootPath, doc.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (!System.IO.File.Exists(path)) return NotFound();
        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, "application/octet-stream", doc.Title);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var doc = await _context.VaultDocuments.FindAsync(id);
        if (doc == null || !ids.Contains(doc.FamilyId)) return NotFound();
        _context.VaultDocuments.Remove(doc);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
