using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data.Shopping
{
    public class ShoppingList
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid FamilyId { get; set; }

        [ForeignKey(nameof(FamilyId))]
        public Family Family { get; set; } = default!;

        [Required, MaxLength(200)]
        [Display(Name = "List Name")]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; } = false;

        public ICollection<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();
    }
}
