using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hearthly.Data;
using Hearthly.Data.Vault;
using Hearthly.Models.Vault;
using System.Security.Claims;

namespace Hearthly.Controllers
{
    [Authorize]
    public class VaultBankAccountsController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDataProtector _protector;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<VaultBankAccountsController> _logger;

        public VaultBankAccountsController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ILogger<VaultBankAccountsController> logger,
    IDataProtectionProvider provider,
    IWebHostEnvironment env)
    : base(context, userManager)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _protector = provider.CreateProtector("VaultBankAccountProtector");
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var accounts = await _context.VaultBankAccounts
                .Where(v => v.UserId == user.Id)
                .ToListAsync();

            // Decrypt full account number for modal; prepare last 4 for display
            foreach (var account in accounts)
            {
                try
                {
                    if (!string.IsNullOrEmpty(account.AccountNumber))
                    {
                        var decrypted = _protector.Unprotect(account.AccountNumber);
                        account.AccountNumber = decrypted; // full account number

                        // Store last 4 into ViewData using a key
                        ViewData[$"Last4_{account.Id}"] = decrypted.Length >= 4
                            ? decrypted[^4..]
                            : decrypted;
                    }
                }
                catch
                {
                    account.AccountNumber = "[Decryption Failed]";
                    ViewData[$"Last4_{account.Id}"] = "[Err]";
                }
            }

            return View(accounts);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VaultBankAccount model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                model.UserId = user.Id;
                model.CreatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(model.AccountNumber))
                {
                    model.AccountNumber = _protector.Protect(model.AccountNumber);
                }

                _context.VaultBankAccounts.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("VaultBankAccount created successfully for user {UserId}", user.Id);
                return RedirectToAction(nameof(Index));
            }

            foreach (var entry in ModelState)
            {
                foreach (var error in entry.Value.Errors)
                {
                    _logger.LogWarning("ModelState error for {Field}: {Error}", entry.Key, error.ErrorMessage);
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var account = await _context.VaultBankAccounts
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == user.Id);

            if (account == null)
            {
                return NotFound();
            }

            try
            {
                if (!string.IsNullOrEmpty(account.AccountNumber))
                {
                    account.AccountNumber = _protector.Unprotect(account.AccountNumber);
                }
            }
            catch
            {
                account.AccountNumber = "[Decryption Failed]";
            }

            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var account = await _context.VaultBankAccounts
                .FirstOrDefaultAsync(v => v.Id == id && v.UserId == user.Id);

            if (account == null)
            {
                TempData["Error"] = "Bank account not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.VaultBankAccounts.Remove(account);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bank account deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> LinkCard(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var account = await _context.VaultBankAccounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (account == null)
                return NotFound();

            var decrypted = string.Empty;
            try
            {
                decrypted = _protector.Unprotect(account.AccountNumber);
            }
            catch
            {
                decrypted = "[Decryption Failed]";
            }

            var vm = new LinkCardViewModel
            {
                AccountId = account.Id,
                AccountHolder = account.AccountHolder,
                BankName = account.BankName.ToString(),
                LastFourDigits = decrypted.Length >= 4 ? decrypted[^4..] : decrypted
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkCard(LinkCardViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            var account = await _context.VaultBankAccounts
                .FirstOrDefaultAsync(a => a.Id == model.AccountId && a.UserId == user.Id);

            if (account == null)
                return NotFound();

            string uploadFolder = Path.Combine(_env.ContentRootPath, "SecureVault", "CardImages");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            if (model.CardFront != null)
            {
                var fileName = $"card_front_{Guid.NewGuid()}{Path.GetExtension(model.CardFront.FileName)}";
                var filePath = Path.Combine(uploadFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await model.CardFront.CopyToAsync(stream);
                account.CardFrontPath = fileName;
            }

            if (model.CardBack != null)
            {
                var fileName = $"card_back_{Guid.NewGuid()}{Path.GetExtension(model.CardBack.FileName)}";
                var filePath = Path.Combine(uploadFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await model.CardBack.CopyToAsync(stream);
                account.CardBackPath = fileName;
            }

            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Card linked successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UnlinkCard(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var account = await _context.VaultBankAccounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (account == null)
            {
                TempData["Error"] = "Bank account not found.";
                return RedirectToAction(nameof(Index));
            }

            // Optionally delete files from disk
            try
            {
                if (!string.IsNullOrEmpty(account.CardFrontPath))
                {
                    var frontPath = Path.Combine(_env.WebRootPath, account.CardFrontPath.TrimStart('/'));
                    if (System.IO.File.Exists(frontPath))
                        System.IO.File.Delete(frontPath);
                }

                if (!string.IsNullOrEmpty(account.CardBackPath))
                {
                    var backPath = Path.Combine(_env.WebRootPath, account.CardBackPath.TrimStart('/'));
                    if (System.IO.File.Exists(backPath))
                        System.IO.File.Delete(backPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not delete card images: {Message}", ex.Message);
            }

            account.CardFrontPath = null;
            account.CardBackPath = null;

            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Linked card removed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCardImage(int id, string side)
        {
            var user = await _userManager.GetUserAsync(User);

            var account = await _context.VaultBankAccounts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);

            if (account == null)
                return NotFound();

            if (!IsPinConfirmedRecently())
            {
                return Unauthorized("Vault PIN not confirmed.");
            }

            string? filePath = side.ToLower() switch
            {
                "front" => account.CardFrontPath,
                "back" => account.CardBackPath,
                _ => null
            };

            if (string.IsNullOrEmpty(filePath))
                return NotFound();

            var absolutePath = Path.Combine(_env.ContentRootPath, "SecureVault", "CardImages", Path.GetFileName(filePath));

            if (!System.IO.File.Exists(absolutePath))
                return NotFound();

            var mimeType = "image/" + Path.GetExtension(absolutePath).TrimStart('.').ToLower();
            return PhysicalFile(absolutePath, mimeType);
        }
    }
}
