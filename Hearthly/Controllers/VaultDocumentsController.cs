using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Data.Vault;

namespace Hearthly.Controllers
{
    [Authorize]
    public class VaultDocumentsController : BaseController
    {
        private readonly ILogger<VaultDocumentsController> _logger;

        public VaultDocumentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<VaultDocumentsController> logger)
            : base(context, userManager)
        {
            _logger = logger;
        }

        private bool VaultUnlocked() =>
            HttpContext.Session.GetString("VaultUnlocked") == "true";

        private byte[]? GetSessionVaultKey()
        {
            var b64 = HttpContext.Session.GetString("VaultKey");
            return b64 == null ? null : Convert.FromBase64String(b64);
        }

        public async Task<IActionResult> Index(DocumentCategory? filter = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (string.IsNullOrEmpty(user?.VaultPinHash))
                return RedirectToAction("SetPin", "Vault");

            if (!VaultUnlocked())
                return RedirectToAction("EnterPin", "Vault");

            var query = _context.VaultDocuments.Where(d => d.UserId == user.Id);

            if (filter.HasValue)
            {
                query = filter == DocumentCategory.Legal
                    ? query.Where(d => d.Category == DocumentCategory.Legal || d.Category == DocumentCategory.Contract)
                    : query.Where(d => d.Category == filter.Value);
            }

            var documents = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();

            ViewBag.Filter = filter;
            return View(documents);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(string title, DocumentCategory category, string? notes, DocumentCategory? filter = null)
        {
            if (!VaultUnlocked()) return RedirectToAction("EnterPin", "Vault");

            var vaultKey = GetSessionVaultKey();
            if (vaultKey == null)
            {
                TempData["UploadError"] = "Vault session expired. Please lock and re-unlock the vault.";
                return RedirectToAction(nameof(Index), filter.HasValue ? new { filter } : null);
            }

            var file = Request.Form.Files["file"];
            if (file == null || file.Length == 0)
            {
                TempData["UploadError"] = "Please select a file.";
                return RedirectToAction(nameof(Index), filter.HasValue ? new { filter } : null);
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["UploadError"] = "Please provide a document title.";
                return RedirectToAction(nameof(Index), filter.HasValue ? new { filter } : null);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["UploadError"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var encryptedData = EncryptionHelper.EncryptData(ms.ToArray(), vaultKey);

            _context.VaultDocuments.Add(new VaultDocument
            {
                Id               = Guid.NewGuid(),
                Title            = title,
                Category         = category,
                Notes            = notes,
                OriginalFileName = file.FileName,
                ContentType      = file.ContentType,
                EncryptedData    = encryptedData,
                UserId           = user.Id,
                UploadedAt       = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["UploadSuccess"] = $"\"{title}\" stored securely in the Vault.";
            return filter.HasValue
                ? RedirectToAction(nameof(Index), new { filter })
                : RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Download(Guid id, DocumentCategory? filter = null)
        {
            if (!VaultUnlocked()) return RedirectToAction("EnterPin", "Vault");

            var vaultKey = GetSessionVaultKey();
            if (vaultKey == null)
            {
                TempData["Error"] = "Vault session expired. Please re-unlock the vault.";
                return RedirectToAction(nameof(Index), filter.HasValue ? new { filter } : null);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var doc = await _context.VaultDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.Id);
            if (doc == null) return NotFound();

            byte[] decrypted;
            try
            {
                decrypted = EncryptionHelper.DecryptData(doc.EncryptedData, vaultKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Decryption failed for document {DocId}", id);
                TempData["Error"] = "Decryption failed — the document may have been encrypted with a different vault key.";
                return RedirectToAction(nameof(Index), filter.HasValue ? new { filter } : null);
            }

            return File(decrypted, doc.ContentType, doc.OriginalFileName);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, DocumentCategory? filter = null)
        {
            if (!VaultUnlocked()) return RedirectToAction("EnterPin", "Vault");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var doc = await _context.VaultDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.UserId == user.Id);
            if (doc == null) return NotFound();

            _context.VaultDocuments.Remove(doc);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"\"{doc.Title}\" has been deleted.";
            return filter.HasValue
                ? RedirectToAction(nameof(Index), new { filter })
                : RedirectToAction(nameof(Index));
        }
    }
}
