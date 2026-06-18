using Nestled.Data;
using Nestled.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nestled.Controllers;

[Authorize]
public class PetsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public PetsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    private async Task<List<int>> GetUserFamilyIds(string userId) =>
        await _context.FamilyMembers.Where(m => m.UserId == userId).Select(m => m.FamilyId).ToListAsync();

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var pets = await _context.Pets.Include(p => p.Family).Where(p => ids.Contains(p.FamilyId)).ToListAsync();
        return View(pets);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Pet pet, IFormFile? photo)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        if (!ids.Contains(pet.FamilyId)) return Forbid();

        if (photo != null && photo.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "uploads", "pets");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await photo.CopyToAsync(stream);
            pet.PhotoPath = $"/uploads/pets/{fileName}";
        }

        ModelState.Remove("Family");
        if (!ModelState.IsValid)
        {
            ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
            return View(pet);
        }

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var pet = await _context.Pets.Include(p => p.CareRecords).Include(p => p.Family).FirstOrDefaultAsync(p => p.Id == id);
        if (pet == null || !ids.Contains(pet.FamilyId)) return NotFound();
        return View(pet);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var pet = await _context.Pets.FindAsync(id);
        if (pet == null || !ids.Contains(pet.FamilyId)) return NotFound();
        ViewBag.Families = await _context.Families.Where(f => ids.Contains(f.Id)).ToListAsync();
        return View(pet);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Pet updated, IFormFile? photo)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var pet = await _context.Pets.FindAsync(id);
        if (pet == null || !ids.Contains(pet.FamilyId)) return NotFound();

        pet.Name = updated.Name;
        pet.Species = updated.Species;
        pet.Breed = updated.Breed;
        pet.BirthDate = updated.BirthDate;
        pet.InsuranceProvider = updated.InsuranceProvider;
        pet.MicrochipNumber = updated.MicrochipNumber;
        pet.IsDeceased = updated.IsDeceased;
        pet.DeceasedDate = updated.DeceasedDate;
        pet.DeceasedNotes = updated.DeceasedNotes;

        if (photo != null && photo.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "uploads", "pets");
            Directory.CreateDirectory(dir);
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
            using var stream = new FileStream(Path.Combine(dir, fileName), FileMode.Create);
            await photo.CopyToAsync(stream);
            pet.PhotoPath = $"/uploads/pets/{fileName}";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var ids = await GetUserFamilyIds(userId);
        var pet = await _context.Pets.FindAsync(id);
        if (pet == null || !ids.Contains(pet.FamilyId)) return NotFound();
        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}
