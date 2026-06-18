using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nestled.Data;

namespace Nestled.Controllers
{
    [Authorize]
    public class ChoresController : BaseController
    {
        public ChoresController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        // GET /Chores/Index?familyId=X
        public async Task<IActionResult> Index(Guid? familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewData["FamilyId"] = familyId;

            if (!familyId.HasValue)
                return View(new List<FamilyChore>());

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            var family = await _context.Families.FindAsync(familyId.Value);
            if (family == null) return NotFound();

            var chores = await _context.FamilyChores
                .Where(c => c.FamilyId == familyId!.Value)
                .OrderBy(c => c.IsDone)
                .ThenBy(c => c.DueDate.HasValue ? 0 : 1)
                .ThenBy(c => c.DueDate)
                .ThenBy(c => c.CreatedAt)
                .ToListAsync();

            // Load assignee profiles for display
            var assigneeIds = chores
                .Where(c => c.AssignedToUserId != null)
                .Select(c => c.AssignedToUserId!)
                .Distinct()
                .ToList();

            var profiles = await _context.UserProfiles
                .Where(p => assigneeIds.Contains(p.UserId))
                .ToListAsync();

            // Load all accepted family members for the assign dropdown
            var memberIds = await _context.FamilyMembers
                .Where(m => m.FamilyId == familyId!.Value && m.IsAccepted)
                .Select(m => m.UserId)
                .ToListAsync();

            var memberProfiles = await _context.UserProfiles
                .Where(p => memberIds.Contains(p.UserId))
                .ToListAsync();

            ViewBag.Family = family;
            ViewBag.FamilyId = familyId!.Value;
            ViewBag.Profiles = profiles;
            ViewBag.MemberProfiles = memberProfiles;
            ViewBag.CurrentUserId = user.Id;

            return View(chores);
        }

        // POST /Chores/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid familyId, string title, string? description,
                                                string? dueDate, string? assignedToUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == familyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false, message = "Not a member." });

            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Title required." });

            DateTime? parsedDue = null;
            if (!string.IsNullOrWhiteSpace(dueDate) && DateTime.TryParse(dueDate, out var d))
                parsedDue = d.Date;

            // Validate assignee is a member of this family
            string? validatedAssigneeId = null;
            if (!string.IsNullOrWhiteSpace(assignedToUserId))
            {
                var assigneeIsMember = await _context.FamilyMembers
                    .AnyAsync(m => m.FamilyId == familyId && m.UserId == assignedToUserId && m.IsAccepted);
                if (assigneeIsMember) validatedAssigneeId = assignedToUserId;
            }

            var chore = new FamilyChore
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                Title = title.Trim()[..Math.Min(title.Trim().Length, 200)],
                Description = description?.Trim(),
                AssignedToUserId = validatedAssigneeId,
                DueDate = parsedDue,
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.FamilyChores.Add(chore);
            await _context.SaveChangesAsync();

            // Build assignee display info for response
            string? assigneeName = null;
            string? assigneePhoto = null;
            if (chore.AssignedToUserId != null)
            {
                var prof = await _context.UserProfiles.FindAsync(chore.AssignedToUserId);
                assigneeName = prof != null ? $"{prof.FirstName} {prof.LastName}".Trim() : null;
                assigneePhoto = prof?.PhotoPath;
            }

            return Json(new
            {
                success = true,
                chore = new
                {
                    id = chore.Id,
                    title = chore.Title,
                    description = chore.Description,
                    isDone = false,
                    dueDate = chore.DueDate?.ToString("yyyy-MM-dd"),
                    dueDateDisplay = chore.DueDate?.ToString("dd MMM yyyy"),
                    assignedToUserId = chore.AssignedToUserId,
                    assigneeName,
                    assigneePhoto
                }
            });
        }

        // POST /Chores/ToggleDone
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDone(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var chore = await _context.FamilyChores.FindAsync(id);
            if (chore == null) return Json(new { success = false });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == chore.FamilyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false });

            chore.IsDone = !chore.IsDone;
            chore.CompletedAt = chore.IsDone ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isDone = chore.IsDone });
        }

        // POST /Chores/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var chore = await _context.FamilyChores.FindAsync(id);
            if (chore == null) return Json(new { success = false });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == chore.FamilyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false });

            _context.FamilyChores.Remove(chore);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST /Chores/Assign
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(Guid id, string? assignedToUserId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var chore = await _context.FamilyChores.FindAsync(id);
            if (chore == null) return Json(new { success = false });

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == chore.FamilyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Json(new { success = false });

            if (string.IsNullOrWhiteSpace(assignedToUserId))
            {
                chore.AssignedToUserId = null;
            }
            else
            {
                var assigneeIsMember = await _context.FamilyMembers
                    .AnyAsync(m => m.FamilyId == chore.FamilyId && m.UserId == assignedToUserId && m.IsAccepted);
                if (!assigneeIsMember) return Json(new { success = false, message = "Assignee is not a family member." });
                chore.AssignedToUserId = assignedToUserId;
            }
            await _context.SaveChangesAsync();

            string? assigneeName = null;
            if (chore.AssignedToUserId != null)
            {
                var prof = await _context.UserProfiles.FindAsync(chore.AssignedToUserId);
                assigneeName = prof != null ? $"{prof.FirstName} {prof.LastName}".Trim() : null;
            }

            return Json(new { success = true, assigneeName });
        }
    }
}
