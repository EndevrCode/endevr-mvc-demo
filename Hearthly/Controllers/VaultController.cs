using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Controllers;
using Hearthly.Data;
using Hearthly.Data.Vault;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

[Authorize]
public class VaultController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VaultController> _logger;
    private readonly IFido2 _fido2;

    public VaultController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<VaultController> logger,
        IFido2 fido2)
        : base(context, userManager)
    {
        _context     = context;
        _userManager = userManager;
        _logger      = logger;
        _fido2       = fido2;
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private bool VaultUnlocked() =>
        HttpContext.Session.GetString("VaultUnlocked") == "true";

    private byte[]? GetSessionVaultKey()
    {
        var b64 = HttpContext.Session.GetString("VaultKey");
        return b64 == null ? null : Convert.FromBase64String(b64);
    }

    private void SetSessionVaultKey(byte[] key) =>
        HttpContext.Session.SetString("VaultKey", Convert.ToBase64String(key));

    // ─── index ──────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrEmpty(user.VaultPinHash))
            return RedirectToAction("SetPin");

        if (!VaultUnlocked())
            return RedirectToAction("EnterPin");

        var files = await _context.VaultFiles
            .Where(f => f.UserId == user.Id)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();

        var biometricCount = await _context.VaultBiometricCredentials
            .CountAsync(c => c.UserId == user.Id);

        ViewData["UserFiles"]      = files;
        ViewBag.BiometricCount     = biometricCount;
        ViewBag.HasSessionVaultKey = GetSessionVaultKey() != null;

        return View();
    }

    // ─── PIN enter / validate ────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> EnterPin()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var hasBiometric = await _context.VaultBiometricCredentials
            .AnyAsync(c => c.UserId == user.Id);

        ViewBag.HasBiometric = hasBiometric;
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
            // Derive and cache vault key for the session lifetime
            if (user.EncryptedVaultKey != null && user.VaultKeySalt != null)
            {
                try
                {
                    using var pbkdf2 = new Rfc2898DeriveBytes(pin, user.VaultKeySalt, 100_000, HashAlgorithmName.SHA256);
                    var derivedKey = pbkdf2.GetBytes(32);
                    var vaultKey   = EncryptionHelper.DecryptData(user.EncryptedVaultKey, derivedKey);
                    SetSessionVaultKey(vaultKey);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not cache vault key for user {UserId}", user.Id);
                }
            }

            HttpContext.Session.SetString("VaultUnlocked", "true");
            return RedirectToAction("Index");
        }

        TempData["Error"] = "Invalid PIN.";
        return RedirectToAction("EnterPin");
    }

    // ─── PIN set / change ────────────────────────────────────────────────────

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

        if (string.IsNullOrWhiteSpace(newPin) || !Regex.IsMatch(newPin, @"^\d{4,6}$"))
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

        // Generate new vault key and re-wrap with new PIN
        var randomKey  = EncryptionHelper.GenerateRandomKey(32);
        var salt       = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(newPin, salt, 100_000, HashAlgorithmName.SHA256);
        var derivedKey = pbkdf2.GetBytes(32);

        var hasherNew = new PasswordHasher<ApplicationUser>();
        user.VaultPinHash      = hasherNew.HashPassword(user, newPin);
        user.EncryptedVaultKey = EncryptionHelper.EncryptData(randomKey, derivedKey);
        user.VaultKeySalt      = salt;

        _context.Update(user);
        await _context.SaveChangesAsync();

        // Update session with new vault key
        SetSessionVaultKey(randomKey);
        HttpContext.Session.SetString("VaultUnlocked", "true");

        TempData["Success"]           = "Vault PIN updated and encryption key saved.";
        TempData["ShowVaultPinWarning"] = true;

        return RedirectToAction("Index", "Vault");
    }

    // ─── Upload ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload()
    {
        if (!VaultUnlocked()) return RedirectToAction("EnterPin");

        var vaultKey = GetSessionVaultKey();
        if (vaultKey == null)
        {
            TempData["UploadError"] = "Vault session expired. Please lock and unlock the vault.";
            return RedirectToAction("Index");
        }

        var file = Request.Form.Files["file"];
        if (file == null || file.Length == 0)
        {
            TempData["UploadError"] = file == null ? "Please select a file." : "The file is empty.";
            return RedirectToAction("Index");
        }

        const long maxFileSize = 20 * 1024 * 1024; // 20 MB
        if (file.Length > maxFileSize)
        {
            TempData["UploadError"] = "File is too large. Maximum upload size is 20 MB.";
            return RedirectToAction("Index");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            TempData["UploadError"] = "User not found.";
            return RedirectToAction("Index");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var encryptedData = EncryptionHelper.EncryptData(memoryStream.ToArray(), vaultKey);

        _context.VaultFiles.Add(new VaultFile
        {
            Id               = Guid.NewGuid(),
            OriginalFileName = file.FileName,
            ContentType      = file.ContentType,
            EncryptedData    = encryptedData,
            UserId           = user.Id,
            UploadedAt       = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        TempData["UploadSuccess"] = "File uploaded securely.";
        return RedirectToAction("Index");
    }

    // ─── Download ────────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Download(Guid id)
    {
        if (!VaultUnlocked()) return RedirectToAction("EnterPin");

        var vaultKey = GetSessionVaultKey();
        if (vaultKey == null)
            return Json(new { success = false, error = "Session expired. Re-unlock the vault." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var file = await _context.VaultFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == user.Id);
        if (file == null) return NotFound();

        byte[] plaintext;
        try { plaintext = EncryptionHelper.DecryptData(file.EncryptedData, vaultKey); }
        catch
        {
            return Json(new { success = false, error = "Decryption failed." });
        }

        return File(plaintext, file.ContentType, file.OriginalFileName);
    }

    // ─── Delete file ─────────────────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        if (!VaultUnlocked())
            return Json(new { success = false, error = "Vault locked." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        var file = await _context.VaultFiles
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == user.Id);
        if (file == null) return Json(new { success = false, error = "Not found." });

        _context.VaultFiles.Remove(file);
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    // ─── Biometric registration ───────────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BiometricRegisterOptions()
    {
        if (!VaultUnlocked())
            return Json(new { success = false, error = "Unlock vault with PIN first." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        var excludedCredentials = await _context.VaultBiometricCredentials
            .Where(c => c.UserId == user.Id)
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToListAsync();

        var fidoUser = new Fido2User
        {
            Id          = Encoding.UTF8.GetBytes(user.Id),
            Name        = user.Email ?? user.UserName ?? "user",
            DisplayName = user.UserName ?? "Vault User"
        };

        var options = _fido2.RequestNewCredential(
            fidoUser,
            excludedCredentials,
            new AuthenticatorSelection
            {
                AuthenticatorAttachment = AuthenticatorAttachment.Platform,
                RequireResidentKey      = false,
                UserVerification        = UserVerificationRequirement.Required
            },
            AttestationConveyancePreference.None);

        HttpContext.Session.SetString("Fido2RegOptions", options.ToJson());
        return Json(options);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BiometricRegisterComplete([FromBody] AuthenticatorAttestationRawResponse attestation)
    {
        var optionsJson = HttpContext.Session.GetString("Fido2RegOptions");
        if (string.IsNullOrEmpty(optionsJson))
            return Json(new { success = false, error = "Registration session expired." });

        var origOptions = CredentialCreateOptions.FromJson(optionsJson);
        HttpContext.Session.Remove("Fido2RegOptions");

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        IsCredentialIdUniqueToUserAsyncDelegate isUnique = async (args, ct) =>
            !await _context.VaultBiometricCredentials
                .AnyAsync(c => c.CredentialId == args.CredentialId, ct);

        Fido2.CredentialMakeResult credential;
        try
        {
            credential = await _fido2.MakeNewCredentialAsync(attestation, origOptions, isUnique);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Biometric registration failed for user {UserId}", user.Id);
            return Json(new { success = false, error = "Registration failed: " + ex.Message });
        }

        _context.VaultBiometricCredentials.Add(new VaultBiometricCredential
        {
            Id               = Guid.NewGuid(),
            UserId           = user.Id,
            CredentialId     = credential.Result.Id,
            PublicKey        = credential.Result.PublicKey,
            SignatureCounter = credential.Result.Counter,
            DeviceName       = "This device",
            RegisteredAt     = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    // ─── Biometric authentication ─────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> BiometricAuthOptions()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        var allowedCredentials = await _context.VaultBiometricCredentials
            .Where(c => c.UserId == user.Id)
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToListAsync();

        if (!allowedCredentials.Any())
            return Json(new { success = false, error = "No biometric credentials registered." });

        var options = _fido2.GetAssertionOptions(allowedCredentials, UserVerificationRequirement.Required);
        HttpContext.Session.SetString("Fido2AuthOptions", options.ToJson());
        return Json(options);
    }

    [HttpPost]
    public async Task<IActionResult> BiometricAuthComplete([FromBody] AuthenticatorAssertionRawResponse clientResponse)
    {
        var optionsJson = HttpContext.Session.GetString("Fido2AuthOptions");
        if (string.IsNullOrEmpty(optionsJson))
            return Json(new { success = false, error = "Auth session expired." });

        var options = AssertionOptions.FromJson(optionsJson);
        HttpContext.Session.Remove("Fido2AuthOptions");

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        var credential = await _context.VaultBiometricCredentials
            .FirstOrDefaultAsync(c => c.UserId == user.Id && c.CredentialId == clientResponse.Id);

        if (credential == null)
            return Json(new { success = false, error = "Credential not found." });

        IsUserHandleOwnerOfCredentialIdAsync isOwner = async (args, ct) =>
            await _context.VaultBiometricCredentials
                .AnyAsync(c => c.CredentialId == args.CredentialId && c.UserId == user.Id, ct);

        AssertionVerificationResult result;
        try
        {
            result = await _fido2.MakeAssertionAsync(
                clientResponse, options, credential.PublicKey, credential.SignatureCounter, isOwner);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Biometric assertion failed for user {UserId}", user.Id);
            return Json(new { success = false, error = "Biometric verification failed." });
        }

        credential.SignatureCounter = result.Counter;
        credential.LastUsedAt       = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        if (GetSessionVaultKey() == null)
            return Json(new { success = false, requirePin = true });

        HttpContext.Session.SetString("VaultUnlocked", "true");
        return Json(new { success = true });
    }

    // ─── Remove biometric credential ─────────────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BiometricRemove(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        var cred = await _context.VaultBiometricCredentials
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);
        if (cred == null) return Json(new { success = false });

        _context.VaultBiometricCredentials.Remove(cred);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BiometricRemoveAll()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Json(new { success = false });

        var creds = await _context.VaultBiometricCredentials
            .Where(c => c.UserId == user.Id)
            .ToListAsync();

        _context.VaultBiometricCredentials.RemoveRange(creds);
        await _context.SaveChangesAsync();
        return Json(new { success = true });
    }

    // ─── PIN validation (AJAX) ───────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidatePinAjax([FromBody] PinCheckRequest request)
    {
        const int maxAttempts = 5;
        const int lockoutMinutes = 15;
        const string attemptsKey = "VaultPinAttempts";
        const string lockoutKey  = "VaultPinLockedUntil";

        // Check lockout
        var lockedUntilStr = HttpContext.Session.GetString(lockoutKey);
        if (lockedUntilStr != null && DateTime.TryParse(lockedUntilStr, out var lockedUntil) && lockedUntil > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((lockedUntil - DateTime.UtcNow).TotalMinutes);
            return Json(new { success = false, locked = true, message = $"Too many failed attempts. Try again in {remaining} minute(s)." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user?.VaultPinHash == null)
            return Json(new { success = false });

        var hasher = new PasswordHasher<ApplicationUser>();
        var result = hasher.VerifyHashedPassword(user, user.VaultPinHash, request.Pin);

        if (result == PasswordVerificationResult.Success)
        {
            HttpContext.Session.Remove(attemptsKey);
            HttpContext.Session.Remove(lockoutKey);
            HttpContext.Session.SetString("VaultPinConfirmed", DateTime.UtcNow.ToString());
            return Json(new { success = true });
        }

        // Increment failure counter
        var attemptsStr = HttpContext.Session.GetString(attemptsKey);
        var attempts = attemptsStr != null && int.TryParse(attemptsStr, out var n) ? n + 1 : 1;
        HttpContext.Session.SetString(attemptsKey, attempts.ToString());

        if (attempts >= maxAttempts)
        {
            HttpContext.Session.SetString(lockoutKey, DateTime.UtcNow.AddMinutes(lockoutMinutes).ToString());
            HttpContext.Session.Remove(attemptsKey);
            return Json(new { success = false, locked = true, message = $"Too many failed attempts. Try again in {lockoutMinutes} minutes." });
        }

        return Json(new { success = false, attemptsLeft = maxAttempts - attempts });
    }

    public class PinCheckRequest
    {
        public string Pin { get; set; } = "";
    }
}
