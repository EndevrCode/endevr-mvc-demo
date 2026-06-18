using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Models;

namespace Hearthly.Controllers
{
    [Authorize]
    public class UtilitiesController : BaseController
    {
        public UtilitiesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        public async Task<IActionResult> Index(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            var utilities = await _context.Utilities
                .Where(u => u.FamilyId == familyId)
                .OrderByDescending(u => u.PurchaseDate)
                .ToListAsync();

            ViewData["FamilyId"] = familyId;
            return View(utilities);
        }

        public async Task<IActionResult> Create(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            ViewData["FamilyId"] = familyId;
            return View(new Utility { FamilyId = familyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid familyId, Utility model)
        {
            model.FamilyId = familyId;
            ModelState.Remove(nameof(model.FamilyId));
            ModelState.Remove(nameof(model.Family));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Background-Sync"] == "true")
                    return BadRequest(ModelState);

                ViewData["FamilyId"] = familyId;
                return View(model);
            }

            _context.Utilities.Add(model);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Background-Sync"] == "true")
                return Ok(new { success = true });

            return RedirectToAction(nameof(Index), new { familyId });
        }

        public async Task<IActionResult> Edit(Guid familyId, int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            var utility = await _context.Utilities
                .FirstOrDefaultAsync(u => u.Id == id && u.FamilyId == familyId);
            if (utility == null) return NotFound();

            ViewData["FamilyId"] = familyId;
            return View(utility);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid familyId, int id, Utility model)
        {
            if (id != model.Id) return BadRequest();

            model.FamilyId = familyId;
            ModelState.Remove(nameof(model.FamilyId));
            ModelState.Remove(nameof(model.Family));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewData["FamilyId"] = familyId;
                return View(model);
            }

            var ent = await _context.Utilities
                .FirstOrDefaultAsync(u => u.Id == id && u.FamilyId == familyId);
            if (ent == null) return NotFound();

            ent.Type = model.Type;
            ent.AmountPaid = model.AmountPaid;
            ent.PurchasedFrom = model.PurchasedFrom;
            ent.AccountUsed = model.AccountUsed;
            ent.PurchaseDate = model.PurchaseDate;
            ent.TokenNumber = model.TokenNumber;
            ent.TotalUnits = model.TotalUnits;

            _context.Update(ent);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { familyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid familyId, int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            var ent = await _context.Utilities
                .FirstOrDefaultAsync(u => u.Id == id && u.FamilyId == familyId);
            if (ent != null)
            {
                _context.Utilities.Remove(ent);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId });
        }

        public async Task<IActionResult> ElectricitySummary(Guid familyId)
            => await Summary(familyId, UtilityType.Electricity);

        public async Task<IActionResult> GasSummary(Guid familyId)
            => await Summary(familyId, UtilityType.Gas);

        private async Task<IActionResult> Summary(Guid familyId, UtilityType type)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            var list = await _context.Utilities
                .Where(u => u.FamilyId == familyId && u.Type == type)
                .OrderBy(u => u.PurchaseDate)
                .ToListAsync();

            var rows = new List<UtilitySummaryRow>();
            DateTime? prevDate = null;

            foreach (var u in list)
            {
                int? daysBetween = null;
                decimal? avgDaily = null;

                if (prevDate.HasValue)
                {
                    daysBetween = (u.PurchaseDate - prevDate.Value).Days;
                    if (u.TotalUnits.HasValue && daysBetween.Value > 0)
                        avgDaily = u.TotalUnits.Value / daysBetween.Value;
                }

                rows.Add(new UtilitySummaryRow
                {
                    PurchaseDate = u.PurchaseDate,
                    UnitsReceived = u.TotalUnits,
                    AmountPaid = u.AmountPaid,
                    PurchasedFrom = u.PurchasedFrom,
                    PreviousPurchase = prevDate,
                    DaysBetween = daysBetween,
                    AverageDailyUsage = avgDaily
                });

                prevDate = u.PurchaseDate;
            }

            ViewData["FamilyId"] = familyId;
            ViewData["UtilityType"] = type;
            return View("Summary", rows);
        }
    }
}
