using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Data.RuleOfThree;
using Hearthly.Models.RuleOfThree;
using System.Security.Claims;

namespace Hearthly.Controllers
{
    [Authorize]
    public class RuleOfThreeController : BaseController
    {
        private readonly ILogger<RuleOfThreeController> _logger;

        public RuleOfThreeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<RuleOfThreeController> logger)
            : base(context, userManager)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var familyIds = await _context.FamilyMembers
                .Where(fm => fm.UserId == userId && fm.IsAccepted)
                .Select(fm => fm.FamilyId)
                .ToListAsync();

            if (familyIds.Count == 1)
            {
                // Only one family — auto-redirect
                return RedirectToAction("Dashboard", new { familyId = familyIds[0] });
            }

            // Correct view name here
            return View("SelectFamilyInfo");
        }

        [HttpPost]
        public async Task<IActionResult> Submit(RuleOfThreeEntry model, Guid? familyId)
        {
            if (!ModelState.IsValid)
                return View("Today", model);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var existing = await _context.RuleOfThreeEntries
                .Include(e => e.Tasks)
                .FirstOrDefaultAsync(e => e.UserId == userId && e.Date == model.Date && e.FamilyId == familyId);

            if (existing != null)
            {
                _context.RuleOfThreeTasks.RemoveRange(existing.Tasks);
                await _context.SaveChangesAsync();

                _context.Entry(existing).State = EntityState.Detached;

                existing = await _context.RuleOfThreeEntries
                    .FirstOrDefaultAsync(e => e.UserId == userId && e.Date == model.Date && e.FamilyId == familyId);
                if (existing == null) return NotFound();

                existing.MainProject = model.MainProject;
                existing.IsPowerDay = model.IsPowerDay;
                existing.FamilyId = familyId; // ensure correct family association

                var newTasks = model.Tasks.Select(task => new RuleOfThreeTask
                {
                    Id = Guid.NewGuid(),
                    EntryId = existing.Id,
                    TaskType = task.TaskType,
                    Description = task.Description,
                    IsDone = task.IsDone,
                    Duration = task.Duration
                }).ToList();

                _context.RuleOfThreeTasks.AddRange(newTasks);
                existing.IsComplete = newTasks.Count == 6 && newTasks.All(t => t.IsDone);
            }
            else
            {
                model.Id = Guid.NewGuid();
                model.UserId = userId;
                model.FamilyId = familyId;

                foreach (var task in model.Tasks)
                {
                    task.Id = Guid.NewGuid();
                    task.EntryId = model.Id;
                }

                model.IsComplete = model.Tasks.Count == 6 && model.Tasks.All(t => t.IsDone);
                _context.RuleOfThreeEntries.Add(model);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Dashboard", new { familyId });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Today(Guid? familyId = null)
        {
            ViewData["FamilyId"] = familyId;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var today = DateTime.Today;

            var entry = await _context.RuleOfThreeEntries
                .Include(e => e.Tasks)
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.Date == today &&
                    (familyId.HasValue ? e.FamilyId == familyId : e.FamilyId == null));

            if (entry != null)
                return View("Today", entry);

            var newEntry = new RuleOfThreeEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = today,
                FamilyId = familyId,
                IsFamilyEntry = familyId.HasValue,
                UsedTimers = 0,
                IsPowerDay = false,
                IsComplete = false,
                StreakAtDay = 0,
                Tasks = Enumerable.Range(0, 6).Select(i => new RuleOfThreeTask
                {
                    Id = Guid.NewGuid(),
                    TaskType = i < 3 ? RuleOfThreeTaskType.Short : RuleOfThreeTaskType.Maintenance,
                    Description = ""
                }).ToList()
            };

            return View("Today", newEntry);
        }


        [Authorize]
        public async Task<IActionResult> Dashboard(Guid? familyId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            // Auto-redirect if the user belongs to only one family
            if (!familyId.HasValue)
            {
                var familyIds = await _context.FamilyMembers
                    .Where(fm => fm.UserId == userId && fm.IsAccepted)
                    .Select(fm => fm.FamilyId)
                    .ToListAsync();

                if (familyIds.Count == 1)
                {
                    // Only one family, redirect with that familyId
                    return RedirectToAction(nameof(Dashboard), new { familyId = familyIds[0] });
                }

                _logger.LogInformation("Dashboard - No family selected. Showing SelectFamilyInfo view.");
                return View("SelectFamilyInfo");
            }

            ViewData["FamilyId"] = familyId;

            var today = DateTime.Today;
            var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var friday = monday.AddDays(4);

            var entries = await _context.RuleOfThreeEntries
                .Where(e => e.UserId == userId &&
                            e.Date >= monday &&
                            e.Date <= friday &&
                            e.FamilyId == familyId)
                .Include(e => e.Tasks)
                .OrderBy(e => e.Date)
                .ToListAsync();

            var todayEntry = entries.FirstOrDefault(e => e.Date == today);

            int dailyProgressPercent = 0;
            if (todayEntry?.Tasks != null && todayEntry.Tasks.Count == 6)
            {
                int doneCount = todayEntry.Tasks.Count(t => t.IsDone);
                dailyProgressPercent = (int)Math.Round((doneCount / 6.0) * 100);
            }

            var model = new RuleOfThreeDashboardViewModel
            {
                Today = today,
                ThisWeekEntries = entries,
                IsTodayComplete = todayEntry?.IsComplete ?? false,
                CurrentStreak = todayEntry?.StreakAtDay ?? 0,
                DailyProgressPercent = dailyProgressPercent
            };

            _logger.LogInformation("Dashboard - Entries found: {Count}. FamilyId: {FamilyId}", entries.Count, familyId);
            return View(model);
        }



        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ViewEntry(DateTime date, Guid? familyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var entry = await _context.RuleOfThreeEntries
                .Include(e => e.Tasks)
                .FirstOrDefaultAsync(e =>
                    e.UserId == userId &&
                    e.Date.Date == date.Date &&
                    (familyId.HasValue ? e.FamilyId == familyId : e.FamilyId == null));

            if (entry == null)
                return NotFound();

            ViewData["ReadOnly"] = true;
            ViewData["FamilyId"] = familyId;
            return View("Today", entry);
        }

        [HttpPost("RuleOfThree/MarkComplete")]
        [Authorize]
        public async Task<IActionResult> MarkComplete(Guid id, Guid? familyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var entry = await _context.RuleOfThreeEntries
                .Include(e => e.Tasks)
                .FirstOrDefaultAsync(e =>
                    e.Id == id &&
                    e.UserId == userId &&
                    (familyId.HasValue ? e.FamilyId == familyId : e.FamilyId == null));

            if (entry == null)
                return NotFound();

            // ✅ Only allow completion if all 6 tasks are done
            if (entry.Tasks.Count == 6 && entry.Tasks.All(t => t.IsDone))
            {
                entry.IsComplete = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Today", new { familyId });
        }

    }
}
