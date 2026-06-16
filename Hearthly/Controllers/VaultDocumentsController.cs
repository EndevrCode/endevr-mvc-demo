using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Data.Vault;
using System.Security.Cryptography;

namespace Hearthly.Controllers
{
    [Authorize]
    public class VaultDocumentsController : BaseController
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<VaultDocumentsController> _logger;

        public VaultDocumentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<VaultDocumentsController> logger)
            : base(context, userManager)
        {
            _protector = dataProtectionProvider.CreateProtector("VaultFileProtector");
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (string.IsNullOrEmpty(user?.VaultPinHash))
                return RedirectToAction("SetPin", "Vault");

            if (HttpContext.Session.GetString("VaultUnlocked") != "true")
                return RedirectToAction("EnterPin", "Vault");

            var documents = await _context.VaultDocuments
                .Where(d => d.UserId == user.Id)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            return View(documents);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string title, DocumentCategory category, string? notes, string pin)
        {
            var file = Request.Form.Files["file"];

            if (file == null || file.Length == 0)
            {
                TempData["UploadError"] = "Please select a file.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["UploadError"] = "Please provide a document title.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user?.EncryptedVaultKey == null || user.VaultKeySalt == null)
            {
                TempData["UploadError"] = "Vault not initialized. Please set a PIN first.";
                return RedirectToAction(nameof(Index));
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(pin, user.VaultKeySalt, 100_000, HashAlgorithmName.SHA256);
            var derivedKey = pbkdf2.GetBytes(32);

            byte[] vaultKey;
            try
            {
                vaultKey = EncryptionHelper.DecryptData(user.EncryptedVaultKey, derivedKey);
            }
            catch
            {
                TempData["UploadError"] = "Incorrect PIN — document not saved.";
                return RedirectToAction(nameof(Index));
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var encryptedData = EncryptionHelper.EncryptData(ms.ToArray(), vaultKey);

            _context.VaultDocuments.Add(new VaultDocument
            {
                Id = Guid.NewGuid(),
                Title = title,
                Category = category,
                Notes = notes,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                EncryptedData = encryptedData,
                UserId = user.Id,
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["UploadSuccess"] = $"\"{title}\" stored securely in the Vault.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Download(Guid id, string pin)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.EncryptedVaultKey == null || user.VaultKeySalt == null)
                return RedirectToAction("EnterPin", "Vault");

            var doc = await _context.VaultDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.Id);

            if (doc == null) return NotFound();

            using var pbkdf2 = new Rfc2898DeriveBytes(pin, user.VaultKeySalt, 100_000, HashAlgorithmName.SHA256);
            var derivedKey = pbkdf2.GetBytes(32);

            try
            {
                var vaultKey = EncryptionHelper.DecryptData(user.EncryptedVaultKey, derivedKey);
                var decrypted = EncryptionHelper.DecryptData(doc.EncryptedData, vaultKey);
                return File(decrypted, doc.ContentType, doc.OriginalFileName);
            }
            catch
            {
                TempData["Error"] = "Incorrect PIN — could not decrypt document.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, string pin)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.EncryptedVaultKey == null || user.VaultKeySalt == null)
                return RedirectToAction("EnterPin", "Vault");

            var doc = await _context.VaultDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.Id);

            if (doc == null) return NotFound();

            using var pbkdf2 = new Rfc2898DeriveBytes(pin, user.VaultKeySalt, 100_000, HashAlgorithmName.SHA256);
            var derivedKey = pbkdf2.GetBytes(32);

            try
            {
                EncryptionHelper.DecryptData(user.EncryptedVaultKey, derivedKey);
            }
            catch
            {
                TempData["Error"] = "Incorrect PIN — document not deleted.";
                return RedirectToAction(nameof(Index));
            }

            _context.VaultDocuments.Remove(doc);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{doc.Title}\" has been deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
