using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Nestled.Data;

namespace Nestled.Controllers
{
    [Authorize]
    public class EmergencyContactsController : BaseController
    {
        public EmergencyContactsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        private async Task<bool> IsFamilyMember(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            return await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
        }

        // GET: /EmergencyContacts?familyId={familyId}
        public async Task<IActionResult> Index(Guid familyId)
        {
            if (!await IsFamilyMember(familyId))
                return Forbid();

            ViewData["FamilyId"] = familyId;

            var contacts = await _context.EmergencyContacts
                .Where(ec => ec.FamilyId == null || ec.FamilyId == familyId)
                .OrderBy(ec => ec.FamilyId)
                .ThenBy(ec => ec.ContactType)
                .ToListAsync();

            return View(contacts);
        }

        // GET: /EmergencyContacts/Create?familyId={familyId}
        public async Task<IActionResult> Create(Guid? familyId)
        {
            if (!familyId.HasValue || !await IsFamilyMember(familyId.Value))
                return BadRequest();

            return View(new EmergencyContact { FamilyId = familyId.Value });
        }

        // POST: /EmergencyContacts/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("FamilyId,ContactType,Name,PhoneNumber,Notes")]
            EmergencyContact emergencyContact)
        {
            if (!emergencyContact.FamilyId.HasValue || !await IsFamilyMember(emergencyContact.FamilyId.Value))
                return Forbid();

            if (!await _context.Families.AnyAsync(f => f.Id == emergencyContact.FamilyId))
            {
                ModelState.AddModelError(nameof(emergencyContact.FamilyId), "Invalid family selected.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(emergencyContact);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { familyId = emergencyContact.FamilyId });
            }

            return View(emergencyContact);
        }

        // POST: /EmergencyContacts/SyncOffline (background sync)
        [HttpPost]
        public async Task<IActionResult> SyncOffline([FromBody] EmergencyContact emergencyContact)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!emergencyContact.FamilyId.HasValue || !await IsFamilyMember(emergencyContact.FamilyId.Value))
                return Forbid();

            emergencyContact.Id = 0;
            _context.EmergencyContacts.Add(emergencyContact);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // GET: /EmergencyContacts/Edit/5?familyId={familyId}
        public async Task<IActionResult> Edit(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsFamilyMember(familyId.Value))
                return BadRequest();

            var contact = await _context.EmergencyContacts
                .FirstOrDefaultAsync(x => x.Id == id && x.FamilyId == familyId);
            if (contact == null) return NotFound();

            ViewData["FamilyId"] = familyId.Value;
            return View(contact);
        }

        // POST: /EmergencyContacts/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Guid? familyId,
            [Bind("Id,FamilyId,ContactType,Name,PhoneNumber,Notes")]
            EmergencyContact emergencyContact)
        {
            if (!familyId.HasValue
                || id != emergencyContact.Id
                || familyId.Value != emergencyContact.FamilyId
                || !await IsFamilyMember(familyId.Value))
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(emergencyContact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.EmergencyContacts.AnyAsync(ec => ec.Id == id && ec.FamilyId == familyId))
                        return NotFound();
                    throw;
                }

                return RedirectToAction(nameof(Index), new { familyId = emergencyContact.FamilyId });
            }

            ViewData["FamilyId"] = familyId;
            return View(emergencyContact);
        }

        // GET: /EmergencyContacts/Delete/5?familyId={familyId}
        public async Task<IActionResult> Delete(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsFamilyMember(familyId.Value))
                return BadRequest();

            var contact = await _context.EmergencyContacts
                .FirstOrDefaultAsync(ec => ec.Id == id && ec.FamilyId == familyId);
            if (contact == null) return NotFound();

            ViewData["FamilyId"] = familyId.Value;
            return View(contact);
        }

        // POST: /EmergencyContacts/Delete/5?familyId={familyId}
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsFamilyMember(familyId.Value))
                return BadRequest();

            var contact = await _context.EmergencyContacts
                .FirstOrDefaultAsync(ec => ec.Id == id && ec.FamilyId == familyId);

            if (contact != null)
            {
                _context.EmergencyContacts.Remove(contact);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId = familyId.Value });
        }
    }
}
