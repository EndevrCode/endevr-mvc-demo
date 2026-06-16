namespace Hearthly.Models;

public class ShoppingList
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;

    public Family? Family { get; set; }
    public ICollection<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();

    public int TotalItems => Items.Count;
    public int CheckedItems => Items.Count(i => i.IsChecked);
}
