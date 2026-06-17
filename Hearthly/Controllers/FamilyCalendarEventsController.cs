using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;

namespace Hearthly.Controllers
{
    [Authorize]
    public class FamilyCalendarEventsController : BaseController
    {
        public FamilyCalendarEventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid familyId, string title, string? description, string date, string color)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false, message = "Not authenticated." });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember)
                return Json(new { success = false, message = "You are not a member of this family." });

            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Title is required." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            var allowed = new[] { "#6366f1", "#10b981", "#f59e0b", "#ef4444", "#3b82f6", "#ec4899", "#8b5cf6" };
            if (!allowed.Contains(color)) color = "#6366f1";

            var ev = new FamilyCalendarEvent
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                Title = title.Trim()[..Math.Min(title.Trim().Length, 200)],
                Description = description?.Trim()[..Math.Min(description.Trim().Length, 500)],
                Date = parsedDate.Date,
                Color = color,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.FamilyCalendarEvents.Add(ev);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                ev = new
                {
                    id = ev.Id,
                    title = ev.Title,
                    start = ev.Date.ToString("yyyy-MM-dd"),
                    color = ev.Color,
                    extendedProps = new { type = "custom", description = ev.Description }
                }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var ev = await _context.FamilyCalendarEvents.FindAsync(id);
            if (ev == null) return Json(new { success = false, message = "Event not found." });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == ev.FamilyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember)
                return Json(new { success = false, message = "Access denied." });

            _context.FamilyCalendarEvents.Remove(ev);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
