using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using System.Security.Claims;

namespace Hearthly.Controllers
{
    [Authorize]
    public class StaffMembersController : BaseController
    {
        private readonly IDataProtector _protector;

        public StaffMembersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IDataProtectionProvider provider)
            : base(context, userManager)
        {
            _protector = provider.CreateProtector("StaffMemberProtector");
        }

        private string? Encrypt(string? value) =>
            string.IsNullOrEmpty(value) ? value : _protector.Protect(value);

        private string? Decrypt(string? value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            try { return _protector.Unprotect(value); }
            catch { return value; } // legacy plaintext fallback
        }

        private void DecryptSensitiveFields(StaffMember s)
        {
            s.IdNumber = Decrypt(s.IdNumber);
            s.PassportNumber = Decrypt(s.PassportNumber);
            s.AccountNumber = Decrypt(s.AccountNumber);
        }

        private void EncryptSensitiveFields(StaffMember s)
        {
            s.IdNumber = Encrypt(s.IdNumber);
            s.PassportNumber = Encrypt(s.PassportNumber);
            s.AccountNumber = Encrypt(s.AccountNumber);
        }

        private async Task<Guid?> GetUserFamilyIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.FamilyMembers
                .Where(fm => fm.UserId == userId && fm.IsAccepted)
                .Select(fm => (Guid?)fm.FamilyId)
                .FirstOrDefaultAsync();
        }

        // GET: StaffMembers?familyId={familyId}
        public async Task<IActionResult> Index(Guid? familyId)
        {
            if (!familyId.HasValue)
                familyId = await GetUserFamilyIdAsync();

            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value))
                return Forbid();

            ViewData["FamilyId"] = familyId.Value;

            var staff = await _context.StaffMembers
                                      .Where(s => s.FamilyId == familyId.Value)
                                      .ToListAsync();

            var staffIds = staff.Select(s => s.Id).ToList();
            var lastPayments = await _context.StaffPayments
                .Where(p => staffIds.Contains(p.StaffMemberId))
                .GroupBy(p => p.StaffMemberId)
                .Select(g => new { StaffMemberId = g.Key, LastDate = g.Max(p => p.PaymentDate), Total = g.Sum(p => p.AmountPaid) })
                .ToListAsync();

            ViewData["LastPayments"] = lastPayments.ToDictionary(
                x => x.StaffMemberId,
                x => (LastDate: x.LastDate, Total: x.Total));

            return View(staff);
        }

        // GET: StaffMembers/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var staffMember = await _context.StaffMembers
                .Include(s => s.Family)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            DecryptSensitiveFields(staffMember);

            // Month-to-date payment summary
            var now = DateTime.Today;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var paidThisMonth = await _context.StaffPayments
                .Where(p => p.StaffMemberId == id && p.PaymentDate >= monthStart && p.PaymentDate < monthEnd)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;

            ViewData["PaidThisMonth"] = paidThisMonth;

            return View(staffMember);
        }

        // GET: StaffMembers/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var familyId = await GetUserFamilyIdAsync();
            if (!familyId.HasValue)
                return RedirectToAction("Index", "Families");

            var model = new StaffMember { FamilyId = familyId.Value };
            ViewData["FamilyId"] = familyId.Value;
            return View(model);
        }

        // POST: StaffMembers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffMember staffMember, IFormFile? photo, string[] WorkDays)
        {
            var familyId = await GetUserFamilyIdAsync();
            if (!familyId.HasValue)
            {
                ModelState.AddModelError("", "You must be part of a family to add staff.");
                return View(staffMember);
            }

            staffMember.FamilyId = familyId.Value;
            ModelState.Remove(nameof(StaffMember.FamilyId));

            if (!ModelState.IsValid)
                return View(staffMember);

            staffMember.Id = Guid.NewGuid();

            if (photo != null && photo.Length > 0)
            {
                var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var photoExt = Path.GetExtension(photo.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(photoExt))
                {
                    ModelState.AddModelError(nameof(photo), "Only JPG, PNG, GIF, and WebP images are allowed.");
                    ViewData["FamilyId"] = staffMember.FamilyId;
                    return View(staffMember);
                }

                const long maxPhotoSize = 5 * 1024 * 1024;
                if (photo.Length > maxPhotoSize)
                {
                    ModelState.AddModelError(nameof(photo), "Photo must be 5 MB or smaller.");
                    ViewData["FamilyId"] = staffMember.FamilyId;
                    return View(staffMember);
                }

                var fileName = $"{Guid.NewGuid()}{photoExt}";
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "staff");
                Directory.CreateDirectory(folder);
                var filePath = Path.Combine(folder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await photo.CopyToAsync(stream);
                staffMember.PhotoPath = $"/uploads/staff/{fileName}";
            }

            staffMember.WorkDays = string.Join(",", WorkDays);

            var exists = await _context.StaffMembers.AnyAsync(s =>
                s.FamilyId == staffMember.FamilyId &&
                s.PreferredName == staffMember.PreferredName &&
                s.ContactNumber == staffMember.ContactNumber);

            if (exists)
            {
                ModelState.AddModelError("", "A staff member with this preferred name and contact number already exists.");
                ViewData["FamilyId"] = staffMember.FamilyId;
                return View(staffMember);
            }

            EncryptSensitiveFields(staffMember);
            _context.Add(staffMember);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { familyId = staffMember.FamilyId });
        }

        // GET: StaffMembers/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var staffMember = await _context.StaffMembers.FindAsync(id);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            DecryptSensitiveFields(staffMember);

            return View(staffMember);
        }

        // POST: StaffMembers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, StaffMember staffMember, IFormFile? photo, string[] workDays)
        {
            if (id != staffMember.Id) return NotFound();

            var existing = await _context.StaffMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            if (!await IsUserInFamily(existing.FamilyId))
                return Forbid();

            staffMember.FamilyId = existing.FamilyId;
            staffMember.WorkDays = (workDays?.Length > 0) ? string.Join(",", workDays) : null;

            if (photo != null && photo.Length > 0)
            {
                var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var photoExt = Path.GetExtension(photo.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(photoExt))
                {
                    ModelState.AddModelError(nameof(photo), "Only JPG, PNG, GIF, and WebP images are allowed.");
                    return View(staffMember);
                }

                const long maxPhotoSize = 5 * 1024 * 1024;
                if (photo.Length > maxPhotoSize)
                {
                    ModelState.AddModelError(nameof(photo), "Photo must be 5 MB or smaller.");
                    return View(staffMember);
                }

                var fileName = $"{Guid.NewGuid()}{photoExt}";
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "staff");
                Directory.CreateDirectory(uploads);
                var filePath = Path.Combine(uploads, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await photo.CopyToAsync(stream);
                staffMember.PhotoPath = $"/uploads/staff/{fileName}";
            }
            else
            {
                staffMember.PhotoPath = existing.PhotoPath;
            }

            if (!ModelState.IsValid)
                return View(staffMember);

            try
            {
                EncryptSensitiveFields(staffMember);
                _context.Update(staffMember);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StaffMemberExists(staffMember.Id)) return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index), new { familyId = staffMember.FamilyId });
        }

        // GET: StaffMembers/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var staffMember = await _context.StaffMembers
                .Include(s => s.Family)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            return View(staffMember);
        }

        // POST: StaffMembers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var staffMember = await _context.StaffMembers.FindAsync(id);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            _context.StaffMembers.Remove(staffMember);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { familyId = staffMember.FamilyId });
        }

        // GET: StaffMembers/PaymentHistory/{id}
        public async Task<IActionResult> PaymentHistory(Guid id)
        {
            var staffMember = await _context.StaffMembers.FindAsync(id);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            var payments = await _context.StaffPayments
                .Where(p => p.StaffMemberId == id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewData["StaffMember"] = staffMember;
            return View(payments);
        }

        // GET: StaffMembers/LogPayment/{id}
        public async Task<IActionResult> LogPayment(Guid id)
        {
            var staffMember = await _context.StaffMembers.FindAsync(id);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            var payment = new StaffPayment
            {
                StaffMemberId = id,
                PaymentDate = DateTime.Today
            };

            ViewData["StaffMember"] = staffMember;
            return View(payment);
        }

        // GET: StaffMembers/Payslip/{paymentId}
        public async Task<IActionResult> Payslip(Guid paymentId)
        {
            var payment = await _context.StaffPayments
                .Include(p => p.StaffMember)
                    .ThenInclude(s => s.Family)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return NotFound();

            if (!await IsUserInFamily(payment.StaffMember.FamilyId))
                return Forbid();

            DecryptSensitiveFields(payment.StaffMember);

            return View(payment);
        }

        // POST: StaffMembers/DeletePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePayment(Guid id)
        {
            var payment = await _context.StaffPayments.FindAsync(id);
            if (payment == null) return NotFound();

            var staffMember = await _context.StaffMembers.FindAsync(payment.StaffMemberId);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            _context.StaffPayments.Remove(payment);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Payment record deleted.";
            return RedirectToAction(nameof(PaymentHistory), new { id = payment.StaffMemberId });
        }

        // POST: StaffMembers/LogPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogPayment(StaffPayment payment)
        {
            var staffMember = await _context.StaffMembers.FindAsync(payment.StaffMemberId);
            if (staffMember == null) return NotFound();

            if (!await IsUserInFamily(staffMember.FamilyId))
                return Forbid();

            ModelState.Remove(nameof(StaffPayment.StaffMember));

            if (!ModelState.IsValid)
            {
                ViewData["StaffMember"] = staffMember;
                return View(payment);
            }

            payment.Id = Guid.NewGuid();
            payment.RecordedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            payment.CreatedAt = DateTime.UtcNow;

            _context.StaffPayments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Payment logged successfully.";
            return RedirectToAction(nameof(PaymentHistory), new { id = payment.StaffMemberId });
        }

        private bool StaffMemberExists(Guid id)
        {
            return _context.StaffMembers.Any(e => e.Id == id);
        }

        private async Task<bool> IsUserInFamily(Guid familyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return await _context.FamilyMembers
                .AnyAsync(fm => fm.FamilyId == familyId && fm.UserId == userId && fm.IsAccepted);
        }
    }
}
