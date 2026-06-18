using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nestled.Data;

namespace Nestled.Controllers
{
    [Authorize]
    public class HealthController : BaseController
    {
        public HealthController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        // GET /Health — own health profile
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var health = await _context.HealthProfiles.FindAsync(user.Id)
                         ?? new HealthProfile { UserId = user.Id };

            return View(health);
        }

        // GET /Health/View/{userId} — view another user's health (must share a family)
        public async Task<IActionResult> View(string userId)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Challenge();

            if (userId == me.Id) return RedirectToAction(nameof(Index));

            var shareFamily = await _context.FamilyMembers
                .Where(m => m.UserId == me.Id && m.IsAccepted)
                .Select(m => m.FamilyId)
                .AnyAsync(fid => _context.FamilyMembers
                    .Any(m2 => m2.FamilyId == fid && m2.UserId == userId && m2.IsAccepted));

            if (!shareFamily) return Forbid();

            var health = await _context.HealthProfiles.FindAsync(userId)
                         ?? new HealthProfile { UserId = userId };

            var profile = await _context.UserProfiles.FindAsync(userId);
            ViewBag.ProfileName = profile != null
                ? $"{profile.FirstName} {profile.LastName}".Trim()
                : userId;

            return View("ViewOther", health);
        }

        // GET /Health/Edit
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var health = await _context.HealthProfiles.FindAsync(user.Id)
                         ?? new HealthProfile { UserId = user.Id };

            return View(health);
        }

        // POST /Health/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HealthProfile model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            model.UserId = user.Id;
            model.UpdatedAt = DateTime.UtcNow;

            ModelState.Remove("User");

            if (!ModelState.IsValid) return View(model);

            var existing = await _context.HealthProfiles.FindAsync(user.Id);
            if (existing == null)
            {
                _context.HealthProfiles.Add(model);
            }
            else
            {
                existing.BloodType = model.BloodType;
                existing.Allergies = model.Allergies;
                existing.CurrentMedications = model.CurrentMedications;
                existing.VaccinationNotes = model.VaccinationNotes;
                existing.MedicalAidName = model.MedicalAidName;
                existing.MedicalAidNumber = model.MedicalAidNumber;
                existing.DoctorName = model.DoctorName;
                existing.DoctorPhone = model.DoctorPhone;
                existing.EmergencyNotes = model.EmergencyNotes;
                existing.UpdatedAt = model.UpdatedAt;
            }

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Health profile updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
