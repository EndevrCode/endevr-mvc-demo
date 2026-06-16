using Hearthly.Data;
using Hearthly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers;

[Authorize]
public class PasswordVaultController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private const string VaultUnlockKey = "VaultUnlockedAt";

    public PasswordVaultController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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

    private static string Encrypt(string text, string key)
    {
        // AES encryption
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key.PadRight(32)[..32]);
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(text);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, aes.IV.Length);
        return Convert.ToBase64String(result);
    }

    private static string Decrypt(string encryptedText, string key)
    {
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key.PadRight(32)[..32]);
        var fullBytes = Convert.FromBase64String(encryptedText);
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = keyBytes;
        var iv = fullBytes[..aes.BlockSize / 8];
        var cipherBytes = fullBytes[(aes.BlockSize / 8)..];
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }

    public async Task<IActionResult> Index()
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var entries = await _context.PasswordVaultEntries.Where(e => ids.Contains(e.FamilyId)).ToListAsync();
        return View(entries);
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
    public async Task<IActionResult> Create(PasswordVaultEntry entry, string plainPassword, string encryptionKey)
    {
        if (!IsVaultUnlocked()) return RedirectToAction("Index", "Vault");
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(entry.FamilyId)) return Forbid();
        entry.UserId = userId;
        entry.EncryptedPassword = Encrypt(plainPassword, encryptionKey);
        entry.CreatedAt = entry.UpdatedAt = DateTime.UtcNow;
        ModelState.Remove("Family");
        ModelState.Remove("User");
        ModelState.Remove("EncryptedPassword");
        _context.PasswordVaultEntries.Add(entry);
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
        var entry = await _context.PasswordVaultEntries.FindAsync(id);
        if (entry == null || !ids.Contains(entry.FamilyId)) return NotFound();
        _context.PasswordVaultEntries.Remove(entry);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
