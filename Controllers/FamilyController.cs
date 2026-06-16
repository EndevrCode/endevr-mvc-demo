using Hearthly.Data;
using Hearthly.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hearthly.Controllers;

[Authorize]
public class FamilyController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FamilyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private async Task<List<int>> GetUserFamilyIds(string userId) =>
        await _context.FamilyMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.FamilyId)
            .ToListAsync();

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var familyIds = await GetUserFamilyIds(userId);
        var families = await _context.Families
            .Include(f => f.Members).ThenInclude(m => m.User)
            .Where(f => familyIds.Contains(f.Id))
            .ToListAsync();
        return View(families);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        var userId = _userManager.GetUserId(User)!;
        var family = new Family { Name = name, Description = description, CreatedByUserId = userId };
        _context.Families.Add(family);
        await _context.SaveChangesAsync();
        _context.FamilyMembers.Add(new FamilyMember { FamilyId = family.Id, UserId = userId, Role = FamilyRole.Admin });
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", new { id = family.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var familyIds = await GetUserFamilyIds(userId);
        if (!familyIds.Contains(id)) return Forbid();

        var family = await _context.Families
            .Include(f => f.Members).ThenInclude(m => m.User)
            .Include(f => f.Invitations)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (family == null) return NotFound();

        var myRole = family.Members.FirstOrDefault(m => m.UserId == userId)?.Role;
        ViewBag.MyRole = myRole;
        return View(family);
    }

    [HttpGet]
    public async Task<IActionResult> Invite(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var member = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == id && m.UserId == userId);
        if (member?.Role != FamilyRole.Admin) return Forbid();
        ViewBag.FamilyId = id;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(int id, string email)
    {
        var userId = _userManager.GetUserId(User)!;
        var member = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == id && m.UserId == userId);
        if (member?.Role != FamilyRole.Admin) return Forbid();

        var invitation = new FamilyInvitation
        {
            FamilyId = id,
            InvitedEmail = email,
            Token = Guid.NewGuid(),
            SentAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        _context.FamilyInvitations.Add(invitation);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Invitation sent to {email}";
        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> AcceptInvite(Guid token)
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.GetUserAsync(User);
        var invite = await _context.FamilyInvitations
            .Include(i => i.Family)
            .FirstOrDefaultAsync(i => i.Token == token && i.Status == InvitationStatus.Pending);

        if (invite == null || invite.ExpiresAt < DateTime.UtcNow)
        {
            TempData["Error"] = "Invitation is invalid or expired.";
            return RedirectToAction("Index");
        }

        if (!string.Equals(invite.InvitedEmail, user?.Email, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "This invitation is for a different email address.";
            return RedirectToAction("Index");
        }

        var existing = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == invite.FamilyId && m.UserId == userId);
        if (existing == null)
        {
            _context.FamilyMembers.Add(new FamilyMember { FamilyId = invite.FamilyId, UserId = userId });
        }
        invite.Status = InvitationStatus.Accepted;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"You joined {invite.Family?.Name}!";
        return RedirectToAction("Details", new { id = invite.FamilyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeInvite(int invitationId)
    {
        var userId = _userManager.GetUserId(User)!;
        var invite = await _context.FamilyInvitations.Include(i => i.Family).FirstOrDefaultAsync(i => i.Id == invitationId);
        if (invite == null) return NotFound();
        var member = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == invite.FamilyId && m.UserId == userId);
        if (member?.Role != FamilyRole.Admin) return Forbid();
        invite.Status = InvitationStatus.Revoked;
        await _context.SaveChangesAsync();
        return RedirectToAction("Details", new { id = invite.FamilyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int familyId, string memberId)
    {
        var userId = _userManager.GetUserId(User)!;
        var myMembership = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (myMembership?.Role != FamilyRole.Admin) return Forbid();
        var target = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == memberId);
        if (target != null) { _context.FamilyMembers.Remove(target); await _context.SaveChangesAsync(); }
        return RedirectToAction("Details", new { id = familyId });
    }
}
