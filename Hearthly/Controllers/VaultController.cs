using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Controllers;
using Hearthly.Data;
using Hearthly.Data.Vault;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

[Authorize]
public class VaultController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VaultController> _logger;

    public VaultController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<VaultController> logger)
        : base(context, userManager)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

        if (string.IsNullOrEmpty(user.VaultPinHash))
            return RedirectToAction("SetPin");

        if (HttpContext.Session.GetString("VaultUnlocked") != "true")
            return RedirectToAction("EnterPin");

        if (TempData["Pin"] is string pin)
        {
            TempData.Remove("Pin"); // PIN consumed; never expose it in ViewBag
        }

        var files = await _context.VaultFiles
            .Where(f => f.UserId == user.Id)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();

        ViewData["UserFiles"] = files;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> EnterPin()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidatePin(string pin)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || user.VaultPinHash == null)
            return RedirectToAction("Index");

        var hasher = new PasswordHasher<ApplicationUser>();
        var result = hasher.VerifyHashedPassword(user, user.VaultPinHash, pin);

        if (result == PasswordVerificationResult.Success)
        {
            HttpContext.Session.SetString("VaultUnlocked", "true");
            TempData["Pin"] = pin;
            return RedirectToAction("Index");
        }

        TempData["Error"] = "Invalid PIN.";
        return RedirectToAction("EnterPin");
    }

    [HttpGet]
    public async Task<IActionResult> SetPin()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.HasExistingPin = user?.VaultPinHash != null;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPin(string oldPin, string newPin, string confirmPin)
    {
        if (newPin != confirmPin)
        {
            TempData["Error"] = "New PIN and Confirm PIN do not match.";
            return RedirectToAction("SetPin");
        }

        if (string.IsNullOrWhiteSpace(newPin) || newPin.Length < 4 || newPin.Length > 6 || !Regex.IsMatch(newPin, @"^\d{4,6}$"))
        {
            TempData["Error"] = "PIN must be 4 to 6 digits only.";
            return RedirectToAction("SetPin");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Index", "Home");

        if (!string.IsNullOrEmpty(user.VaultPinHash))
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var result = hasher.VerifyHashedPassword(user, user.VaultPinHash, oldPin);
            if (result != PasswordVerificationResult.Success)
            {
                TempData["Error"] = "Incorrect current PIN.";
                return RedirectToAction("SetPin");
            }
        }

        var hasherNew = new PasswordHasher<ApplicationUser>();
        user.VaultPinHash = hasherNew.HashPassword(user, newPin);

        var randomKey = EncryptionHelper.GenerateRandomKey(32);
        var salt = RandomNumberGenerator.GetBytes(16);

        using var pbkdf2 = new Rfc2898DeriveBytes(newPin, salt, 100_000, HashAlgorithmName.SHA256);
        var derivedKey = pbkdf2.GetBytes(32);

        user.EncryptedVaultKey = EncryptionHelper.EncryptData(randomKey, derivedKey);
        user.VaultKeySalt = salt;

        _context.Update(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Vault PIN updated and encryption key saved.";
        TempData["ShowVaultPinWarning"] = true;

        return RedirectToAction("Index", "Vault");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(string pin)
    {
        var file = Request.Form.Files["file"];

        if (file == null || file.Length == 0)
        {
            TempData["UploadError"] = file == null ? "Please select a file." : "The file is empty.";
            return RedirectToAction("Index");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["UploadError"] = "User not found.";
            return RedirectToAction("Index");
        }

        if (user.EncryptedVaultKey == null || user.VaultKeySalt == null)
        {
            TempData["UploadError"] = "Vault not initialized.";
            return RedirectToAction("Index");
        }

        using var pbkdf2 = new Rfc2898DeriveBytes(pin, user.VaultKeySalt, 100_000, HashAlgorithmName.SHA256);
        var derivedKey = pbkdf2.GetBytes(32);

        byte[] decryptedVaultKey;
        try
        {
            decryptedVaultKey = EncryptionHelper.DecryptData(user.EncryptedVaultKey, derivedKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vault key decryption failed for user {UserId} — likely incorrect PIN.", user.Id);
            TempData["UploadError"] = "Incorrect PIN or corrupted encryption key.";
            return RedirectToAction("Index");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        var encryptedData = EncryptionHelper.EncryptData(fileBytes, decryptedVaultKey);

        var vaultFile = new VaultFile
        {
            Id = Guid.NewGuid(),
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            EncryptedData = encryptedData,
            UserId = user.Id,
            UploadedAt = DateTime.UtcNow
        };

        _context.VaultFiles.Add(vaultFile);
        await _context.SaveChangesAsync();

        TempData["UploadSuccess"] = "File uploaded securely.";
        return RedirectToAction("Index");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Download(Guid id, string pin)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (HttpContext.Session.GetString("VaultUnlocked") != "true")
            return RedirectToAction("EnterPin");

        var file = await _context.VaultFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == user.Id);
        if (file == null) return NotFound();

        using var pbkdf2 = new Rfc2898DeriveBytes(pin, user.VaultKeySalt!, 100_000, HashAlgorithmName.SHA256);
        var derivedKey = pbkdf2.GetBytes(32);

        byte[] vaultKey;
        try { vaultKey = EncryptionHelper.DecryptData(user.EncryptedVaultKey!, derivedKey); }
        catch
        {
            return Json(new { success = false, error = "Incorrect PIN." });
        }

        byte[] plaintext;
        try { plaintext = EncryptionHelper.DecryptData(file.EncryptedData, vaultKey); }
        catch
        {
            return Json(new { success = false, error = "Decryption failed." });
        }

        return File(plaintext, file.ContentType, file.OriginalFileName);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(Guid id, string pin)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        if (HttpContext.Session.GetString("VaultUnlocked") != "true")
            return Json(new { success = false, error = "Vault locked." });

        var file = await _context.VaultFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == user.Id);
        if (file == null) return Json(new { success = false, error = "Not found." });

        var hasher = new PasswordHasher<ApplicationUser>();
        var result  = hasher.VerifyHashedPassword(user, user.VaultPinHash ?? "", pin);
        if (result != PasswordVerificationResult.Success)
            return Json(new { success = false, error = "Incorrect PIN." });

        _context.VaultFiles.Remove(file);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidatePinAjax([FromBody] PinCheckRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.VaultPinHash == null)
            return Json(new { success = false });

        var hasher = new PasswordHasher<ApplicationUser>();
        var result = hasher.VerifyHashedPassword(user, user.VaultPinHash, request.Pin);

        if (result == PasswordVerificationResult.Success)
        {
            HttpContext.Session.SetString("VaultPinConfirmed", DateTime.UtcNow.ToString());
            return Json(new { success = true });
        }

        return Json(new { success = false });
    }

    public class PinCheckRequest
    {
        public string Pin { get; set; }
    }
}
