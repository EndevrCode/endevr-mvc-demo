using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;

namespace Hearthly.Controllers
{
    [Authorize]
    public class DashboardController : BaseController
    {
        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        // GET: /Dashboard
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1) Families I'm an accepted member of
            var myFamilyIds = await _context.FamilyMembers
                .Where(m => m.UserId == user.Id && m.IsAccepted)
                .Select(m => m.FamilyId)
                .Distinct()
                .ToListAsync();

            // Load families, members, profiles & pets
            var families = await _context.Families
                .Where(f => myFamilyIds.Contains(f.Id))
                .Include(f => f.Members.Where(m => m.IsAccepted))
                    .ThenInclude(m => m.User)
                .ToListAsync();

            // Gather all member userIds to load profiles in bulk
            var memberUserIds = families
                .SelectMany(f => f.Members)
                .Select(m => m.UserId)
                .Distinct()
                .ToList();

            var profiles = await _context.UserProfiles
                .Where(p => memberUserIds.Contains(p.UserId))
                .ToListAsync();

            // Build FamilyInfo view‑models
            var familyInfos = new List<FamilyInfo>();
            foreach (var f in families)
            {
                var pets = await _context.Pets
                    .Where(p => p.FamilyId == f.Id)
                    .ToListAsync();

                familyInfos.Add(new FamilyInfo
                {
                    Family = f,
                    Members = f.Members.Select(m =>
                    {
                        var prof = profiles.FirstOrDefault(p => p.UserId == m.UserId);
                        return new MemberInfo
                        {
                            UserId = m.UserId,
                            Email = m.User.Email,
                            PreferredName = prof?.PreferredName,
                            Role = m.Role,
                            IsAccepted = m.IsAccepted,
                            PhotoPath = prof?.PhotoPath
                        };
                    }).ToList(),
                    Pets = pets
                });
            }

            // 2) Recent invites I created in the last 7 days
            var recentInvites = await _context.FamilyInvites
                .Where(i => myFamilyIds.Contains(i.FamilyId)
                         && i.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            // 3) Pending invites for me
            var pendingInvites = await _context.FamilyInvites
                .Where(i => i.InvitedEmail == user.Email
                         && i.ExpiresAt > DateTime.UtcNow)
                .Include(i => i.Family)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();

            // Build calendar events
            var events = new List<object>();

            // Upcoming birthdays
            var acceptedMembers = await _context.FamilyMembers
                .Where(m => myFamilyIds.Contains(m.FamilyId) && m.IsAccepted)
                .Include(m => m.User)
                .ToListAsync();

            foreach (var member in acceptedMembers)
            {
                var prof = profiles.FirstOrDefault(p => p.UserId == member.UserId);
                if (prof?.BirthDate != null)
                {
                    var bd = prof.BirthDate.Value;
                    var today = DateTime.UtcNow.Date;
                    var thisYear = new DateTime(today.Year, bd.Month, bd.Day);
                    var nextBd = thisYear >= today
                                 ? thisYear
                                 : thisYear.AddYears(1);

                    events.Add(new
                    {
                        title = $"🎂 {prof.FirstName} {prof.LastName}'s Birthday",
                        start = nextBd.ToString("yyyy-MM-dd"),
                        allDay = true
                    });
                }
            }

            // Recent invites events
            foreach (var inv in recentInvites)
            {
                events.Add(new
                {
                    title = $"Invite sent to {inv.InvitedEmail}",
                    start = inv.CreatedAt.ToString("yyyy-MM-dd"),
                    allDay = true
                });
            }

            // 4) Bills summary
            var unpaidBills = await _context.Bills
                .Where(b => myFamilyIds.Contains(b.FamilyId) && !b.IsPaid)
                .ToListAsync();

            var overdueBills = unpaidBills
                .Where(b => b.DueDate.Date < DateTime.Today)
                .OrderBy(b => b.DueDate)
                .ToList();

            // Assemble the view‑model
            var vm = new DashboardViewModel
            {
                Families = familyInfos,
                RecentInvites = recentInvites,
                PendingInvites = pendingInvites,
                UnpaidBillsCount = unpaidBills.Count,
                UnpaidBillsTotal = unpaidBills.Sum(b => b.Amount),
                OverdueBills = overdueBills,
                EventsJson = JsonSerializer.Serialize(events)
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptInvite(Guid token)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invite = await _context.FamilyInvites
                .FirstOrDefaultAsync(i => i.Token == token
                                       && i.ExpiresAt > DateTime.UtcNow);
            if (invite == null) return NotFound();

            _context.FamilyMembers.Add(new FamilyMember
            {
                FamilyId = invite.FamilyId,
                UserId = user.Id,
                Role = IdentityRoles.Member,
                IsAccepted = true,
                JoinedAt = DateTime.UtcNow
            });

            _context.FamilyInvites.Remove(invite);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
