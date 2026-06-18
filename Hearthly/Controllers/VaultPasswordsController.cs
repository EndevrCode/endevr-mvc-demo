using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Data.Vault;

namespace Hearthly.Controllers
{
    [Authorize]
    public class VaultPasswordsController : BaseController
    {
        private new readonly ApplicationDbContext _context;
        private new readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IDataProtector _protector;

        public VaultPasswordsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env,
            IDataProtectionProvider provider)
            : base(context, userManager)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _protector = provider.CreateProtector("VaultPasswordProtector");
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var passwords = await _context.VaultPasswords
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Clear sensitive fields — credentials are served via PIN-gated AJAX
            foreach (var item in passwords)
            {
                item.Password = null;
                item.Username = null;
            }

            return View(passwords);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> GetPassword(Guid id)
        {
            if (!IsPinConfirmedRecently())
                return Json(new { success = false, message = "PIN confirmation required." });

            var userId = _userManager.GetUserId(User);
            var entry = await _context.VaultPasswords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (entry == null)
                return Json(new { success = false, message = "Entry not found." });

            string? decryptedPassword = null;
            if (!string.IsNullOrEmpty(entry.Password))
            {
                try { decryptedPassword = _protector.Unprotect(entry.Password); }
                catch { decryptedPassword = "[Decryption Failed]"; }
            }

            return Json(new { success = true, username = entry.Username, password = decryptedPassword });
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VaultPassword password)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Validation failed.";
                return View();
            }

            password.UserId = user.Id;
            password.Section = VaultSection.Passwords;
            password.CreatedAt = DateTime.UtcNow;

            // Encrypt password before saving
            if (!string.IsNullOrEmpty(password.Password))
            {
                password.Password = _protector.Protect(password.Password);
            }

            _context.VaultPasswords.Add(password);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);

            if (file == null || file.Length == 0 || user == null)
            {
                TempData["Error"] = "Invalid file or user.";
                return RedirectToAction(nameof(Create));
            }

            var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv",
                  ".jpg", ".jpeg", ".png", ".gif", ".webp", ".zip" };
            var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(fileExt))
            {
                TempData["Error"] = "File type not allowed. Upload PDF, Office documents, images, text, CSV, or ZIP files.";
                return RedirectToAction(nameof(Create));
            }

            const long maxFileSize = 20 * 1024 * 1024; // 20 MB
            if (file.Length > maxFileSize)
            {
                TempData["Error"] = "File is too large. Maximum upload size is 20 MB.";
                return RedirectToAction(nameof(Create));
            }

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "SecureVault", "VaultFiles");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{fileExt}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var vaultPassword = new VaultPassword
            {
                UserId = user.Id,
                Section = VaultSection.Passwords,
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                PasswordType = PasswordType.Other,
                CreatedAt = DateTime.UtcNow,
                FilePath = uniqueFileName
            };

            _context.VaultPasswords.Add(vaultPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password document uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (!IsPinConfirmedRecently())
            {
                TempData["Error"] = "Please re-enter your Vault PIN to edit this entry.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var password = await _context.VaultPasswords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (password == null)
            {
                TempData["Error"] = "Password entry not found.";
                return RedirectToAction("Index");
            }

            // Decrypt so the edit form shows the plain-text value
            if (!string.IsNullOrEmpty(password.Password))
            {
                try { password.Password = _protector.Unprotect(password.Password); }
                catch { password.Password = ""; }
            }

            return View(password);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, VaultPassword updatedPassword)
        {
            if (!IsPinConfirmedRecently())
            {
                TempData["Error"] = "Please re-enter your Vault PIN to edit this entry.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var existing = await _context.VaultPasswords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (existing == null)
            {
                TempData["Error"] = "Password entry not found.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                return View(updatedPassword);
            }

            existing.Title = updatedPassword.Title;
            existing.Username = updatedPassword.Username;
            existing.Notes = updatedPassword.Notes;
            existing.PasswordType = updatedPassword.PasswordType;

            // Always re-encrypt the password before saving
            if (!string.IsNullOrEmpty(updatedPassword.Password))
            {
                existing.Password = _protector.Protect(updatedPassword.Password);
            }

            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult MarkPinConfirmed()
        {
            try
            {
                HttpContext.Session.SetString("VaultPinConfirmed", DateTime.UtcNow.ToString("o"));
                return Ok();
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            if (!IsPinConfirmedRecently())
            {
                TempData["Error"] = "Please unlock the vault to download files.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            var entry = await _context.VaultPasswords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (entry == null || string.IsNullOrEmpty(entry.FilePath))
                return NotFound();

            string absolutePath;
            if (entry.FilePath.StartsWith('/') || entry.FilePath.StartsWith('\\'))
                absolutePath = Path.Combine(_env.WebRootPath, entry.FilePath.TrimStart('/', '\\'));
            else
                absolutePath = Path.Combine(_env.ContentRootPath, "SecureVault", "VaultFiles", entry.FilePath);

            if (!System.IO.File.Exists(absolutePath))
                return NotFound();

            var contentType = "application/octet-stream";
            var fileName = entry.Title + Path.GetExtension(absolutePath);
            return PhysicalFile(absolutePath, contentType, fileName);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!IsPinConfirmedRecently())
            {
                return Json(new { success = false, message = "PIN confirmation required." });
            }

            var userId = _userManager.GetUserId(User);
            var entry = await _context.VaultPasswords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (entry == null)
            {
                return Json(new { success = false, message = "Entry not found." });
            }

            _context.VaultPasswords.Remove(entry);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
