using Hearthly.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers
{
    [Authorize]
    public class BillsController : BaseController
    {
        public BillsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager) { }

        private async Task<bool> IsUserInFamily(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            return await _context.FamilyMembers
                .AnyAsync(fm => fm.FamilyId == familyId && fm.UserId == user.Id && fm.IsAccepted);
        }

        // GET: /Bills/Index?familyId=...
        public async Task<IActionResult> Index(Guid? familyId)
        {
            if (!familyId.HasValue) return View(Enumerable.Empty<Bill>());
            if (!await IsUserInFamily(familyId.Value)) return Forbid();

            ViewData["FamilyId"] = familyId.Value;

            var bills = await _context.Bills
                .Where(b => b.FamilyId == familyId.Value)
                .OrderBy(b => b.IsPaid)
                .ThenBy(b => b.DueDate)
                .ToListAsync();

            return View(bills);
        }

        // GET: /Bills/Create?familyId=...
        public async Task<IActionResult> Create(Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value)) return Forbid();

            ViewData["FamilyId"] = familyId.Value;
            return View(new Bill { FamilyId = familyId.Value, DueDate = DateTime.Today.AddDays(30) });
        }

        // POST: /Bills/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FamilyId,Name,Category,Amount,DueDate,IsRecurring,Notes")] Bill bill)
        {
            if (!await IsUserInFamily(bill.FamilyId)) return Forbid();

            bill.CreatedAt = DateTime.UtcNow;

            if (ModelState.IsValid)
            {
                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { familyId = bill.FamilyId });
            }

            ViewData["FamilyId"] = bill.FamilyId;
            return View(bill);
        }

        // GET: /Bills/Edit/5?familyId=...
        public async Task<IActionResult> Edit(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value)) return Forbid();

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.FamilyId == familyId.Value);

            if (bill == null) return NotFound();

            ViewData["FamilyId"] = familyId.Value;
            return View(bill);
        }

        // POST: /Bills/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Guid? familyId,
            [Bind("Id,FamilyId,Name,Category,Amount,DueDate,IsPaid,PaidDate,IsRecurring,Notes")] Bill bill)
        {
            if (!familyId.HasValue || id != bill.Id || familyId.Value != bill.FamilyId) return BadRequest();
            if (!await IsUserInFamily(familyId.Value)) return Forbid();

            if (bill.IsPaid && !bill.PaidDate.HasValue)
                bill.PaidDate = DateTime.UtcNow;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bill);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Bills.AnyAsync(b => b.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index), new { familyId = bill.FamilyId });
            }

            ViewData["FamilyId"] = familyId.Value;
            return View(bill);
        }

        // POST: /Bills/MarkPaid/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id, Guid familyId)
        {
            if (!await IsUserInFamily(familyId)) return Json(new { success = false });

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.FamilyId == familyId);

            if (bill == null) return Json(new { success = false });

            var wasUnpaid = !bill.IsPaid;
            bill.IsPaid   = !bill.IsPaid;
            bill.PaidDate = bill.IsPaid ? DateTime.UtcNow : (DateTime?)null;

            bool nextBillCreated = false;

            // Auto-generate next month's bill when a recurring bill is marked paid
            if (bill.IsPaid && bill.IsRecurring && wasUnpaid)
            {
                var nextDue = bill.DueDate.AddMonths(1);
                var alreadyExists = await _context.Bills
                    .AnyAsync(b => b.FamilyId == familyId
                               && b.Name == bill.Name
                               && b.DueDate.Year  == nextDue.Year
                               && b.DueDate.Month == nextDue.Month);

                if (!alreadyExists)
                {
                    _context.Bills.Add(new Bill
                    {
                        FamilyId    = familyId,
                        Name        = bill.Name,
                        Category    = bill.Category,
                        Amount      = bill.Amount,
                        DueDate     = nextDue,
                        IsRecurring = true,
                        Notes       = bill.Notes,
                        CreatedAt   = DateTime.UtcNow
                    });
                    nextBillCreated = true;
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isPaid = bill.IsPaid, nextBillCreated });
        }

        // GET: /Bills/Delete/5?familyId=...
        public async Task<IActionResult> Delete(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value)) return Forbid();

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.FamilyId == familyId.Value);

            if (bill == null) return NotFound();

            ViewData["FamilyId"] = familyId.Value;
            return View(bill);
        }

        // POST: /Bills/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value)) return Forbid();

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.FamilyId == familyId.Value);

            if (bill != null)
            {
                _context.Bills.Remove(bill);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId = familyId!.Value });
        }
    }
}
