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
            var passwords = await _context.VaultPasswords
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Decrypt passwords
            foreach (var item in passwords)
            {
                if (!string.IsNullOrEmpty(item.Password))
                {
                    try
                    {
                        item.Password = _protector.Unprotect(item.Password);
                    }
                    catch
                    {
                        item.Password = "[Decryption Failed]";
                    }
                }
            }

            return View(passwords);
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

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "vault");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var vaultPassword = new VaultPassword
            {
                UserId = user.Id,
                Section = VaultSection.Passwords,
                Title = file.FileName,
                PasswordType = PasswordType.Other,
                CreatedAt = DateTime.UtcNow,
                FilePath = $"/uploads/vault/{uniqueFileName}"
            };

            _context.VaultPasswords.Add(vaultPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password document uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> VerifyPin(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
                return Json(new { success = false });

            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.VaultPinHash))
                return Json(new { success = false });

            bool isValid = BCrypt.Net.BCrypt.Verify(pin, user.VaultPinHash);
            return Json(new { success = isValid });
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
            var password = await _context.VaultPasswords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

            if (password == null)
            {
                TempData["Error"] = "Password entry not found.";
                return RedirectToAction("Index");
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
            existing.Password = updatedPassword.Password;
            existing.Notes = updatedPassword.Notes;
            existing.PasswordType = updatedPassword.PasswordType;

            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("Index");
        }

        private bool IsPinConfirmedRecently()
        {
            var lastConfirmed = HttpContext.Session.GetString("VaultPinConfirmed");
            return lastConfirmed != null && DateTime.TryParse(lastConfirmed, out var confirmedTime) && confirmedTime > DateTime.UtcNow.AddMinutes(-5);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult MarkPinConfirmed()
        {
            try
            {
                HttpContext.Session.SetString("VaultPinConfirmed", DateTime.UtcNow.ToString("o"));
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Vault PIN confirmation error: " + ex.Message);
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            if (!HttpContext.Session.TryGetValue("VaultPinConfirmed", out _))
            {
                return RedirectToAction("Index");
            }

            var entry = await _context.VaultPasswords
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (entry == null)
            {
                return NotFound();
            }

            _context.VaultPasswords.Remove(entry);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password entry deleted.";
            return RedirectToAction("Index");
        }
    }
}
