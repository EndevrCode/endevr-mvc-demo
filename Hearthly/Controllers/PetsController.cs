using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data;
using Hearthly.Models;
using Hearthly.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hearthly.Controllers
{
    [Authorize]
    public class PetsController : BaseController
    {
        private readonly IWebHostEnvironment _environment;

        public PetsController(ApplicationDbContext context,
                              UserManager<ApplicationUser> userManager,
                              IWebHostEnvironment environment)
            : base(context, userManager)
        {
            _environment = environment;
        }

        // Helper: get list of FamilyIds this user is allowed in
        private async Task<List<Guid>> GetAllowedFamilyIdsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return await _context.FamilyMembers
                .Where(fm => fm.UserId == user.Id && fm.IsAccepted)
                .Select(fm => fm.FamilyId)
                .ToListAsync();
        }

        // GET: Pets?familyId={familyId}
        public async Task<IActionResult> Index(Guid? familyId)
        {
            ViewData["FamilyId"] = familyId;
            if (!familyId.HasValue)
                return View(new List<Pet>());

            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(familyId.Value))
                return Forbid();

            var pets = await _context.Pets
                                     .Where(p => p.FamilyId == familyId.Value)
                                     .ToListAsync();

            return View(pets);
        }

        // GET: Pets/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var pet = await _context.Pets
                .Include(p => p.Family)
                .FirstOrDefaultAsync(m => m.Id == id.Value);
            if (pet == null) return NotFound();

            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(pet.FamilyId)) return Forbid();

            return View(pet);
        }

        // GET: Pets/Create
        public async Task<IActionResult> Create()
        {
            var allowed = await GetAllowedFamilyIdsAsync();

            ViewBag.FamilyList = await _context.Families
                .Where(f => allowed.Contains(f.Id))
                .ToListAsync();

            return View(new PetCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PetCreateViewModel model)
        {
            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(model.FamilyId)) return Forbid();

            // Ensure photo is provided
            if (model.PhotoFile == null || model.PhotoFile.Length == 0)
            {
                ModelState.AddModelError("PhotoFile", "A photo is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FamilyList = await _context.Families
                    .Where(f => allowed.Contains(f.Id))
                    .ToListAsync();
                return View(model);
            }

            var pet = new Pet
            {
                Id = Guid.NewGuid(),
                FamilyId = model.FamilyId,
                Name = model.Name,
                Species = model.Species,
                Breed = model.Breed,
                BirthDate = model.BirthDate,
                LastWeightKg = model.LastWeightKg,
                LastWeighedDate = model.LastWeighedDate,
                LastDewormingDate = model.LastDewormingDate,
                LastTickFleaDate = model.LastTickFleaDate,
                LastGroomingDate = model.LastGroomingDate,
                LastCheckupDate = model.LastCheckupDate,
                HasInsurance = model.HasInsurance,
                InsuranceNumber = model.InsuranceNumber,
                IsMicrochipped = model.IsMicrochipped,
                MicrochipNumber = model.MicrochipNumber,
                IsDeceased = model.IsDeceased,
                DateOfDeath = model.DateOfDeath,
            };

            if (model.PhotoFile != null && model.PhotoFile.Length > 0)
            {
                var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var petPhotoExt = Path.GetExtension(model.PhotoFile.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(petPhotoExt))
                {
                    ModelState.AddModelError("PhotoFile", "Only JPG, PNG, GIF, and WebP images are allowed.");
                    return View(model);
                }

                var uploads = Path.Combine(_environment.WebRootPath, "images/pets");
                Directory.CreateDirectory(uploads);
                var fileName = $"{Guid.NewGuid()}{petPhotoExt}";
                var filePath = Path.Combine(uploads, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await model.PhotoFile.CopyToAsync(stream);
                pet.PhotoPath = $"/images/pets/{fileName}";
            }

            _context.Add(pet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { familyId = pet.FamilyId });
        }

        // POST: Pets/SyncOffline (background sync support)
        [HttpPost]
        public async Task<IActionResult> SyncOffline([FromBody] Pet pet)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == pet.FamilyId && m.UserId == user.Id && m.IsAccepted);
            if (!isMember) return Forbid();

            pet.Id = Guid.NewGuid();
            _context.Pets.Add(pet);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // GET: Pets/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var pet = await _context.Pets.FindAsync(id.Value);
            if (pet == null) return NotFound();

            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(pet.FamilyId)) return Forbid();

            var families = await _context.Families
                .Where(f => allowed.Contains(f.Id))
                .ToListAsync();

            ViewBag.FamilyOptions = new SelectList(families, "Id", "Name", pet.FamilyId);

            return View(new PetEditViewModel
            {
                Pet = pet,
                FamilyId = pet.FamilyId,
                PhotoPath = pet.PhotoPath
            });
        }

        // POST: Pets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PetEditViewModel model)
        {
            var pet = model.Pet;
            var photoFile = model.PhotoFile;

            if (id != pet.Id) return NotFound();

            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(pet.FamilyId)) return Forbid();

            var existing = await _context.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existing == null) return NotFound();

            if (photoFile == null && string.IsNullOrEmpty(existing.PhotoPath))
            {
                ModelState.AddModelError("PhotoFile", "The PhotoFile field is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FamilyOptions = new SelectList(
                    await _context.Families.Where(f => allowed.Contains(f.Id)).ToListAsync(),
                    "Id", "Name", model.FamilyId);
                return View(model);
            }

            if (photoFile != null && photoFile.Length > 0)
            {
                var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var petEditPhotoExt = Path.GetExtension(photoFile.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(petEditPhotoExt))
                {
                    ModelState.AddModelError("PhotoFile", "Only JPG, PNG, GIF, and WebP images are allowed.");
                    return View(model);
                }

                var uploads = Path.Combine(_environment.WebRootPath, "images/pets");
                Directory.CreateDirectory(uploads);
                var fileName = $"{Guid.NewGuid()}{petEditPhotoExt}";
                var filePath = Path.Combine(uploads, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await photoFile.CopyToAsync(stream);
                pet.PhotoPath = $"/images/pets/{fileName}";
            }
            else
            {
                pet.PhotoPath = existing.PhotoPath;
            }

            _context.Update(pet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { familyId = pet.FamilyId });
        }

        // GET: Pets/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var pet = await _context.Pets
                .Include(p => p.Family)
                .FirstOrDefaultAsync(m => m.Id == id.Value);
            if (pet == null) return NotFound();

            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(pet.FamilyId)) return Forbid();

            return View(pet);
        }

        // POST: Pets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet == null) return NotFound();

            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(pet.FamilyId)) return Forbid();

            _context.Pets.Remove(pet);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { familyId = pet.FamilyId });
        }

        // POST: Pets/LogCare
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> LogCare(Guid id, string careType)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet == null) return Json(new { success = false });

            var allowed = await GetAllowedFamilyIdsAsync();
            if (!allowed.Contains(pet.FamilyId)) return Json(new { success = false });

            var today = DateTime.Today;
            switch (careType)
            {
                case "Deworming":  pet.LastDewormingDate = today; break;
                case "Tick & Flea": pet.LastTickFleaDate = today; break;
                case "Grooming":   pet.LastGroomingDate  = today; break;
                case "Checkup":    pet.LastCheckupDate   = today; break;
                default: return Json(new { success = false, message = "Unknown care type." });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, dateStr = today.ToString("dd MMM yyyy") });
        }

        // GET: Pets/Remembrance
        public async Task<IActionResult> Remembrance()
        {
            var allowed = await GetAllowedFamilyIdsAsync();
            var deceasedPets = await _context.Pets
                .Where(p => p.IsDeceased && allowed.Contains(p.FamilyId))
                .ToListAsync();
            return View(deceasedPets);
        }
    }
}
