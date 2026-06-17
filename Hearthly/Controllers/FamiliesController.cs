using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using System.Collections.Generic;

namespace Hearthly.Controllers
{
    [Authorize]
    public class FamiliesController : BaseController
    {
        public FamiliesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        // GET: Families
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var allowedFamilyIds = await _context.FamilyMembers
                .Where(fm => fm.UserId == user.Id && fm.IsAccepted)
                .Select(fm => fm.FamilyId)
                .ToListAsync();

            var families = await _context.Families
                .Include(f => f.CreatedBy).ThenInclude(u => u.Profile)
                .Where(f => allowedFamilyIds.Contains(f.Id))
                .ToListAsync();

            return View(families);
        }

        // GET: Families/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var family = await _context.Families
                .Include(f => f.CreatedBy)
                .Include(f => f.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(f => f.Id == id.Value);
            if (family == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isMember = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id.Value && m.UserId == userId && m.IsAccepted);
            if (!isMember) return Forbid();

            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id.Value && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            ViewData["IsAdmin"] = isAdmin;

            var acceptedMembers = family.Members
                .Where(m => m.IsAccepted).ToList();

            var profileUserIds = acceptedMembers
                .Select(m => m.UserId)
                .Distinct()
                .ToList();

            var profiles = await _context.UserProfiles
                .Where(p => profileUserIds.Contains(p.UserId))
                .ToListAsync();

            var pets = await _context.Pets
                .Where(p => p.FamilyId == family.Id)
                .ToListAsync();

            var vm = new FamilyInfo
            {
                Family = family,
                Members = acceptedMembers.Select(m =>
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
            };

            return View(vm);
        }

        // GET: Families/Create
        public IActionResult Create() => View();

        // POST: Families/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            var user = await _userManager.GetUserAsync(User);

            // Prevent accidental background sync duplication
            var exists = await _context.Families.AnyAsync(f =>
                f.Name == name && f.CreatedById == user.Id);
            if (exists)
                return RedirectToAction(nameof(Index));

            var family = new Family
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Families.Add(family);
            _context.FamilyMembers.Add(new FamilyMember
            {
                FamilyId = family.Id,
                UserId = user.Id,
                Role = IdentityRoles.Admin,
                IsAccepted = true,
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Families/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id.Value && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            var family = await _context.Families.FindAsync(id.Value);
            if (family == null) return NotFound();
            return View(family);
        }

        // POST: Families/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,CreatedById,CreatedAt")] Family family)
        {
            if (id != family.Id) return NotFound();
            if (!ModelState.IsValid) return View(family);

            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            try
            {
                _context.Update(family);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Families.AnyAsync(e => e.Id == family.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Families/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id.Value && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            var family = await _context.Families
                .Include(f => f.CreatedBy)
                .FirstOrDefaultAsync(f => f.Id == id.Value);
            if (family == null) return NotFound();
            return View(family);
        }

        // POST: Families/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            // Check if other accepted members still exist
            var acceptedMembers = await _context.FamilyMembers
                .Where(m => m.FamilyId == id && m.IsAccepted)
                .ToListAsync();

            if (acceptedMembers.Count > 1)
            {
                TempData["SweetError"] = "You can't delete this family while other members are still part of it. Please remove or ask them to leave first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Proceed with deletion
            _context.FamilyMembers.RemoveRange(
                _context.FamilyMembers.Where(m => m.FamilyId == id));
            _context.FamilyInvites.RemoveRange(
                _context.FamilyInvites.Where(i => i.FamilyId == id));
            var family = await _context.Families.FindAsync(id);
            if (family != null) _context.Families.Remove(family);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Families/Invite/5
        [HttpGet]
        public async Task<IActionResult> Invite(Guid? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id.Value && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            var family = await _context.Families.FindAsync(id.Value);
            if (family == null) return NotFound();

            ViewData["FamilyName"] = family.Name;
            ViewData["FamilyId"] = family.Id;
            return View();
        }

        // POST: Families/Invite/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(Guid id, string invitedEmail)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            if (string.IsNullOrWhiteSpace(invitedEmail))
                ModelState.AddModelError(nameof(invitedEmail), "Email is required.");

            var family = await _context.Families.FindAsync(id);
            if (family == null) return NotFound();

            ViewData["FamilyName"] = family.Name;
            ViewData["FamilyId"] = family.Id;

            if (!ModelState.IsValid)
                return View();

            var old = await _context.FamilyInvites
                .FirstOrDefaultAsync(i => i.FamilyId == id && i.InvitedEmail == invitedEmail);
            if (old != null)
                _context.FamilyInvites.Remove(old);

            var token = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var expiry = now.AddDays(7);
            _context.FamilyInvites.Add(new FamilyInvite
            {
                Token = token,
                FamilyId = id,
                InvitedEmail = invitedEmail,
                CreatedAt = now,
                ExpiresAt = expiry
            });

            await _context.SaveChangesAsync();

            ViewData["InviteToken"] = token;
            ViewData["InviteEmail"] = invitedEmail;
            ViewData["InviteExpires"] = expiry;
            return View("InviteResult");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(Guid familyId, string userId, string newRole)
        {
            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == familyId && m.UserId == currentUserId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            var member = await _context.FamilyMembers
                .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId && m.IsAccepted);
            if (member == null) return NotFound();

            if (member.Role == IdentityRoles.Admin && newRole != IdentityRoles.Admin)
            {
                var otherAdmins = await _context.FamilyMembers
                    .CountAsync(m => m.FamilyId == familyId && m.Role == IdentityRoles.Admin && m.IsAccepted && m.UserId != userId);

                if (otherAdmins == 0)
                {
                    TempData["ErrorMessage"] = "You must assign another admin before removing the last one.";
                    return RedirectToAction(nameof(Details), new { id = familyId });
                }
            }

            member.Role = newRole;
            _context.Update(member);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Role updated successfully.";
            return RedirectToAction(nameof(Details), new { id = familyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(Guid id, string userId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Get the member you're trying to remove
            var member = await _context.FamilyMembers
                .FirstOrDefaultAsync(m =>
                    m.FamilyId == id &&
                    m.UserId == userId &&
                    m.IsAccepted);

            if (member == null)
                return NotFound();

            // Check if the current user is removing themselves
            bool isSelf = currentUserId == userId;

            // Count accepted members
            var totalAccepted = await _context.FamilyMembers
                .CountAsync(m => m.FamilyId == id && m.IsAccepted);

            // If self-removal and last member, delete family
            if (isSelf && totalAccepted == 1)
            {
                var family = await _context.Families.FindAsync(id);
                if (family != null)
                {
                    _context.FamilyMembers.RemoveRange(
                        _context.FamilyMembers.Where(m => m.FamilyId == id));

                    _context.FamilyInvites.RemoveRange(
                        _context.FamilyInvites.Where(i => i.FamilyId == id));

                    _context.Families.Remove(family);

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", "Families");
                }
            }

            // Otherwise, remove the member
            _context.FamilyMembers.Remove(member);
            await _context.SaveChangesAsync();

            // Redirect appropriately
            if (isSelf)
                return RedirectToAction("Index", "Families");

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Families/Invites/{id}
        [HttpGet]
        public async Task<IActionResult> Invites(Guid? id)
        {
            if (id == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == id.Value && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            var invites = await _context.FamilyInvites
                .Where(i => i.FamilyId == id.Value)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewData["FamilyId"] = id.Value;
            return View(invites);
        }

        // POST: Families/Revoke
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(Guid token)
        {
            var invite = await _context.FamilyInvites.FirstOrDefaultAsync(i => i.Token == token);
            if (invite == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == invite.FamilyId && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            _context.FamilyInvites.Remove(invite);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Invite revoked.";
            return RedirectToAction(nameof(Invites), new { id = invite.FamilyId });
        }

        // POST: Families/Resend
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend(Guid token)
        {
            var invite = await _context.FamilyInvites.FirstOrDefaultAsync(i => i.Token == token);
            if (invite == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var isAdmin = await _context.FamilyMembers.AnyAsync(m =>
                m.FamilyId == invite.FamilyId && m.UserId == userId &&
                m.Role == IdentityRoles.Admin && m.IsAccepted);
            if (!isAdmin) return Forbid();

            var email = invite.InvitedEmail;
            var familyId = invite.FamilyId;
            _context.FamilyInvites.Remove(invite);

            var newToken = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var expiry = now.AddDays(7);
            _context.FamilyInvites.Add(new FamilyInvite
            {
                Token = newToken,
                FamilyId = familyId,
                InvitedEmail = email,
                CreatedAt = now,
                ExpiresAt = expiry
            });
            await _context.SaveChangesAsync();

            ViewData["InviteToken"] = newToken;
            ViewData["InviteEmail"] = email;
            ViewData["InviteExpires"] = expiry;
            return View("InviteResult");
        }

    }
}
