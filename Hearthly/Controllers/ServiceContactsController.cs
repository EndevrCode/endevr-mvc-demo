using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Hearthly.Data;

namespace Hearthly.Controllers
{
    [Authorize]
    public class ServiceContactsController : BaseController
    {
        public ServiceContactsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        private async Task<bool> IsUserInFamily(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            return await _context.FamilyMembers
                .AnyAsync(fm => fm.FamilyId == familyId && fm.UserId == user.Id && fm.IsAccepted);
        }

        // GET: ServiceContacts?familyId={familyId}
        public async Task<IActionResult> Index(Guid? familyId)
        {
            if (!familyId.HasValue) return View(Enumerable.Empty<ServiceContact>());

            if (!await IsUserInFamily(familyId.Value)) return Forbid();

            ViewData["FamilyId"] = familyId.Value;

            var contacts = await _context.ServiceContacts
                                         .Where(sc => sc.FamilyId == familyId.Value)
                                         .ToListAsync();
            return View(contacts);
        }

        // GET: /ServiceContacts/Details/5?familyId={familyId}
        public async Task<IActionResult> Details(int id, Guid? familyId)
        {
            if (!familyId.HasValue) return BadRequest();
            if (!await IsUserInFamily(familyId.Value)) return Forbid();

            var contact = await _context.ServiceContacts
                .Include(sc => sc.Family)
                .FirstOrDefaultAsync(sc => sc.Id == id && sc.FamilyId == familyId);

            if (contact == null) return NotFound();

            ViewData["FamilyId"] = familyId.Value;
            return View(contact);
        }

        // GET: /ServiceContacts/Create?familyId={familyId}
        public async Task<IActionResult> Create(Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value))
                return Forbid();

            var vm = new ServiceContact { FamilyId = familyId.Value };
            return View(vm);
        }

        // POST: /ServiceContacts/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FamilyId,ServiceType,Company,ContactPerson,ContactNumber,Notes")] ServiceContact serviceContact)
        {
            if (!await IsUserInFamily(serviceContact.FamilyId))
                return Forbid();

            if (!await _context.Families.AnyAsync(f => f.Id == serviceContact.FamilyId))
                ModelState.AddModelError(nameof(serviceContact.FamilyId), "Invalid family selected.");

            if (ModelState.IsValid)
            {
                _context.Add(serviceContact);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { familyId = serviceContact.FamilyId });
            }

            return View(serviceContact);
        }

        // GET: /ServiceContacts/Edit/5?familyId={familyId}
        public async Task<IActionResult> Edit(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value))
                return Forbid();

            var contact = await _context.ServiceContacts
                .FirstOrDefaultAsync(sc => sc.Id == id && sc.FamilyId == familyId);

            if (contact == null) return NotFound();

            ViewData["FamilyId"] = familyId.Value;
            return View(contact);
        }

        // POST: /ServiceContacts/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Guid? familyId,
            [Bind("Id,FamilyId,ServiceType,Company,ContactPerson,ContactNumber,Notes")]
            ServiceContact serviceContact)
        {
            if (!familyId.HasValue || id != serviceContact.Id || familyId.Value != serviceContact.FamilyId)
                return BadRequest();

            if (!await IsUserInFamily(familyId.Value)) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(serviceContact);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.ServiceContacts
                            .AnyAsync(sc => sc.Id == id && sc.FamilyId == familyId))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index), new { familyId = serviceContact.FamilyId });
            }

            ViewData["FamilyId"] = familyId.Value;
            return View(serviceContact);
        }

        // GET: /ServiceContacts/Delete/5?familyId={familyId}
        public async Task<IActionResult> Delete(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value))
                return Forbid();

            var contact = await _context.ServiceContacts
                .FirstOrDefaultAsync(sc => sc.Id == id && sc.FamilyId == familyId);

            if (contact == null) return NotFound();

            ViewData["FamilyId"] = familyId.Value;
            return View(contact);
        }

        // POST: /ServiceContacts/Delete/5?familyId={familyId}
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, Guid? familyId)
        {
            if (!familyId.HasValue || !await IsUserInFamily(familyId.Value))
                return Forbid();

            var contact = await _context.ServiceContacts
                .FirstOrDefaultAsync(sc => sc.Id == id && sc.FamilyId == familyId);

            if (contact != null)
            {
                _context.ServiceContacts.Remove(contact);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId = familyId.Value });
        }
    }
}
