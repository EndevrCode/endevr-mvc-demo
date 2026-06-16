namespace Hearthly.Models;

public class ShoppingItem
{
    public int Id { get; set; }
    public int ShoppingListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; }
    public bool IsChecked { get; set; } = false;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public ShoppingList? ShoppingList { get; set; }
}
