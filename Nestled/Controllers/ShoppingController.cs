using Nestled.Data;
using Nestled.Data.Shopping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nestled.Controllers
{
    [Authorize]
    public class ShoppingController : BaseController
    {
        public ShoppingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager) { }

        private async Task<bool> IsUserInFamily(Guid familyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            return await _context.FamilyMembers
                .AnyAsync(fm => fm.FamilyId == familyId && fm.UserId == user.Id && fm.IsAccepted);
        }

        // GET: /Shopping/Index?familyId=...
        public async Task<IActionResult> Index(Guid? familyId)
        {
            if (!familyId.HasValue) return View(Enumerable.Empty<ShoppingList>());
            if (!await IsUserInFamily(familyId.Value)) return Forbid();

            ViewData["FamilyId"] = familyId.Value;

            var lists = await _context.ShoppingLists
                .Where(l => l.FamilyId == familyId.Value && !l.IsArchived)
                .Include(l => l.Items)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(lists);
        }

        // POST: /Shopping/CreateList
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateList(Guid familyId, string name)
        {
            if (!await IsUserInFamily(familyId)) return Forbid();

            if (!string.IsNullOrWhiteSpace(name))
            {
                var list = new ShoppingList
                {
                    FamilyId = familyId,
                    Name = name.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.ShoppingLists.Add(list);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId });
        }

        // POST: /Shopping/AddItem
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int listId, Guid familyId, string name, string? quantity)
        {
            if (!await IsUserInFamily(familyId)) return Json(new { success = false, error = "Forbidden" });

            var list = await _context.ShoppingLists
                .FirstOrDefaultAsync(l => l.Id == listId && l.FamilyId == familyId);

            if (list == null) return Json(new { success = false, error = "List not found" });

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, error = "Name required" });

            var maxSort = await _context.ShoppingItems
                .Where(i => i.ShoppingListId == listId)
                .MaxAsync(i => (int?)i.SortOrder) ?? 0;

            var item = new ShoppingItem
            {
                ShoppingListId = listId,
                Name           = name.Trim(),
                Quantity       = string.IsNullOrWhiteSpace(quantity) ? null : quantity.Trim(),
                SortOrder      = maxSort + 1,
                AddedAt        = DateTime.UtcNow
            };
            _context.ShoppingItems.Add(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = item.Id, name = item.Name, quantity = item.Quantity });
        }

        // POST: /Shopping/ToggleItem
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleItem(int itemId, Guid familyId)
        {
            if (!await IsUserInFamily(familyId)) return Json(new { success = false });

            var item = await _context.ShoppingItems
                .Include(i => i.ShoppingList)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.ShoppingList.FamilyId == familyId);

            if (item == null) return Json(new { success = false });

            item.IsChecked = !item.IsChecked;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isChecked = item.IsChecked, itemId });
        }

        // POST: /Shopping/DeleteItem
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int itemId, Guid familyId)
        {
            if (!await IsUserInFamily(familyId)) return Json(new { success = false });

            var item = await _context.ShoppingItems
                .Include(i => i.ShoppingList)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.ShoppingList.FamilyId == familyId);

            if (item != null)
            {
                _context.ShoppingItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        // POST: /Shopping/ClearChecked
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearChecked(int listId, Guid familyId)
        {
            if (!await IsUserInFamily(familyId)) return Forbid();

            var checked_ = await _context.ShoppingItems
                .Include(i => i.ShoppingList)
                .Where(i => i.ShoppingListId == listId && i.ShoppingList.FamilyId == familyId && i.IsChecked)
                .ToListAsync();

            _context.ShoppingItems.RemoveRange(checked_);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { familyId });
        }

        // POST: /Shopping/ArchiveList
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveList(int listId, Guid familyId)
        {
            if (!await IsUserInFamily(familyId)) return Forbid();

            var list = await _context.ShoppingLists
                .FirstOrDefaultAsync(l => l.Id == listId && l.FamilyId == familyId);

            if (list != null)
            {
                list.IsArchived = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId });
        }

        // POST: /Shopping/DeleteList
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteList(int listId, Guid familyId)
        {
            if (!await IsUserInFamily(familyId)) return Forbid();

            var list = await _context.ShoppingLists
                .Include(l => l.Items)
                .FirstOrDefaultAsync(l => l.Id == listId && l.FamilyId == familyId);

            if (list != null)
            {
                _context.ShoppingLists.Remove(list);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { familyId });
        }
    }
}
