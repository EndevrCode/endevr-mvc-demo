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

            var selectedId = familyId.HasValue && myFamilyIds.Contains(familyId.Value)
                ? familyId.Value
                : myFamilyIds.First();

            var families = await _context.Families
                .Where(f => myFamilyIds.Contains(f.Id))
                .ToListAsync();

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

            var places = await _context.FamilyPlaces
                .Where(p => p.FamilyId == selectedId)
                .ToListAsync();

            var myLocation = locations.FirstOrDefault(l => l.UserId == user.Id);

            ViewBag.FamilyId  = selectedId;
            ViewBag.Families  = families;
            ViewBag.Members   = members;
            ViewBag.Profiles  = profiles;
            ViewBag.Locations = locations;
            ViewBag.Places    = places;
            ViewBag.MyLocation    = myLocation;
            ViewBag.CurrentUserId = user.Id;

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(
            Guid familyId,
            double? latitude,
            double? longitude,
            string? address,
            string? note,
            int? batteryLevel,
            bool? isCharging,
            double? speed)
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
                    Id       = Guid.NewGuid(),
                    UserId   = user.Id,
                    FamilyId = familyId,
                    IsVisible = true
                };
                _context.FamilyLocations.Add(location);
            }

            if (latitude.HasValue && longitude.HasValue)
            {
                location.Latitude  = latitude;
                location.Longitude = longitude;

                // Detect nearest saved place within its radius
                var places = await _context.FamilyPlaces
                    .Where(p => p.FamilyId == familyId)
                    .ToListAsync();

                location.PlaceName = null;
                foreach (var place in places)
                {
                    if (HaversineMeters(latitude.Value, longitude.Value, place.Latitude, place.Longitude) <= place.RadiusMeters)
                    {
                        location.PlaceName = place.Name;
                        break;
                    }
                }
            }

            if (address != null)         location.Address  = address;
            if (note != null)            location.Note     = note;
            if (batteryLevel.HasValue)   location.BatteryLevel = batteryLevel;
            if (isCharging.HasValue)     location.IsCharging   = isCharging;
            if (speed.HasValue)          location.Speed        = speed;

            location.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, placeName = location.PlaceName });
        }

        [HttpGet]
        public async Task<IActionResult> GetLocations(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false });

            var members = await _context.FamilyMembers
                .Where(m => m.FamilyId == familyId && m.IsAccepted)
                .Include(m => m.User)
                .ToListAsync();

            var memberIds = members.Select(m => m.UserId).ToList();

            var profiles = await _context.UserProfiles
                .Where(p => memberIds.Contains(p.UserId))
                .ToListAsync();

            var locations = await _context.FamilyLocations
                .Where(l => l.FamilyId == familyId && memberIds.Contains(l.UserId))
                .ToListAsync();

            var data = members.Select(m =>
            {
                var prof = profiles.FirstOrDefault(p => p.UserId == m.UserId);
                var loc  = locations.FirstOrDefault(l => l.UserId == m.UserId);
                var name = prof?.PreferredName ?? prof?.FirstName ?? m.User?.UserName ?? "Member";
                var visible = loc?.IsVisible ?? true;
                return new
                {
                    userId      = m.UserId,
                    name,
                    initials    = name.Length > 0 ? name[..1].ToUpper() : "?",
                    photoPath   = prof?.PhotoPath,
                    latitude    = visible ? loc?.Latitude   : (double?)null,
                    longitude   = visible ? loc?.Longitude  : (double?)null,
                    address     = visible ? loc?.Address    : null,
                    placeName   = visible ? loc?.PlaceName  : null,
                    note        = loc?.Note,
                    batteryLevel = loc?.BatteryLevel,
                    isCharging  = loc?.IsCharging,
                    speed       = loc?.Speed,
                    updatedAt   = loc?.UpdatedAt,
                    isVisible   = visible,
                    isMe        = m.UserId == user.Id
                };
            });

            return Json(new { success = true, members = data });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNote(Guid familyId, string? note)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var location = await _context.FamilyLocations
                .FirstOrDefaultAsync(l => l.FamilyId == familyId && l.UserId == user.Id);

            if (location != null)
            {
                location.Note      = note?.Trim();
                location.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var location = await _context.FamilyLocations
                .FirstOrDefaultAsync(l => l.FamilyId == familyId && l.UserId == user.Id);

            if (location == null)
            {
                location = new FamilyLocation
                {
                    Id        = Guid.NewGuid(),
                    UserId    = user.Id,
                    FamilyId  = familyId,
                    IsVisible = false,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.FamilyLocations.Add(location);
            }
            else
            {
                location.IsVisible = !location.IsVisible;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, isVisible = location.IsVisible });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePlace(
            Guid familyId,
            string name,
            FamilyPlaceType placeType,
            double latitude,
            double longitude,
            int radiusMeters = 200)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false, error = "Not a family member." });

            var place = new FamilyPlace
            {
                Id              = Guid.NewGuid(),
                FamilyId        = familyId,
                Name            = name.Trim(),
                PlaceType       = placeType,
                Latitude        = latitude,
                Longitude       = longitude,
                RadiusMeters    = Math.Clamp(radiusMeters, 50, 5000),
                CreatedByUserId = user.Id,
                CreatedAt       = DateTime.UtcNow
            };

            _context.FamilyPlaces.Add(place);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success      = true,
                id           = place.Id,
                name         = place.Name,
                placeType    = (int)place.PlaceType,
                latitude     = place.Latitude,
                longitude    = place.Longitude,
                radiusMeters = place.RadiusMeters
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlace(Guid id, Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var place = await _context.FamilyPlaces
                .FirstOrDefaultAsync(p => p.Id == id && p.FamilyId == familyId);

            if (place == null) return Json(new { success = false, error = "Not found." });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false, error = "Not a family member." });

            _context.FamilyPlaces.Remove(place);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
