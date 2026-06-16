using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hearthly.Data.Shopping
{
    public class ShoppingItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShoppingListId { get; set; }

        [ForeignKey(nameof(ShoppingListId))]
        public ShoppingList ShoppingList { get; set; } = default!;

        [Required, MaxLength(300)]
        [Display(Name = "Item")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Quantity")]
        public string? Quantity { get; set; }

        [Display(Name = "Checked")]
        public bool IsChecked { get; set; } = false;

        public int SortOrder { get; set; } = 0;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
