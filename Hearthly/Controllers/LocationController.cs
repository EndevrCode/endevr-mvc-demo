using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;

namespace Hearthly.Controllers
{
    [Authorize]
    public class LocationController : BaseController
    {
        public LocationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager) { }

        public async Task<IActionResult> Index(Guid? familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var myFamilyIds = await _context.FamilyMembers
                .Where(m => m.UserId == user.Id && m.IsAccepted)
                .Select(m => m.FamilyId)
                .ToListAsync();

            if (!myFamilyIds.Any())
            {
                ViewBag.NoFamily = true;
                return View();
            }

            // Default to first family if none specified or invalid
            var selectedId = familyId.HasValue && myFamilyIds.Contains(familyId.Value)
                ? familyId.Value
                : myFamilyIds.First();

            var families = await _context.Families
                .Where(f => myFamilyIds.Contains(f.Id))
                .ToListAsync();

            // Load all accepted members of the selected family
            var members = await _context.FamilyMembers
                .Where(m => m.FamilyId == selectedId && m.IsAccepted)
                .Include(m => m.User)
                .ToListAsync();

            var memberIds = members.Select(m => m.UserId).ToList();

            var profiles = await _context.UserProfiles
                .Where(p => memberIds.Contains(p.UserId))
                .ToListAsync();

            var locations = await _context.FamilyLocations
                .Where(l => l.FamilyId == selectedId && memberIds.Contains(l.UserId))
                .ToListAsync();

            var myLocation = locations.FirstOrDefault(l => l.UserId == user.Id);

            ViewBag.FamilyId = selectedId;
            ViewBag.Families = families;
            ViewBag.Members = members;
            ViewBag.Profiles = profiles;
            ViewBag.Locations = locations;
            ViewBag.MyLocation = myLocation;
            ViewBag.CurrentUserId = user.Id;

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(
            Guid familyId,
            double? latitude,
            double? longitude,
            string? address,
            string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false, error = "Not a family member." });

            var location = await _context.FamilyLocations
                .FirstOrDefaultAsync(l => l.FamilyId == familyId && l.UserId == user.Id);

            if (location == null)
            {
                location = new FamilyLocation
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    FamilyId = familyId,
                    IsVisible = true
                };
                _context.FamilyLocations.Add(location);
            }

            if (latitude.HasValue && longitude.HasValue)
            {
                location.Latitude = latitude;
                location.Longitude = longitude;
            }

            if (address != null) location.Address = address;
            if (note != null) location.Note = note;
            location.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNote(Guid familyId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));

            var location = await _context.FamilyLocations
                .FirstOrDefaultAsync(l => l.FamilyId == familyId && l.UserId == user.Id);

            if (location != null)
            {
                location.Note = note?.Trim();
                location.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));

            var location = await _context.FamilyLocations
                .FirstOrDefaultAsync(l => l.FamilyId == familyId && l.UserId == user.Id);

            if (location != null)
            {
                location.IsVisible = !location.IsVisible;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId });
        }
    }
}
