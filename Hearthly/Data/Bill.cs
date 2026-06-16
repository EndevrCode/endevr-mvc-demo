using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hearthly.Data
{
    public class Bill
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid FamilyId { get; set; }

        [ForeignKey(nameof(FamilyId))]
        public Family Family { get; set; } = default!;

        [Required, MaxLength(200)]
        [Display(Name = "Bill Name")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Paid")]
        public bool IsPaid { get; set; } = false;

        [Display(Name = "Date Paid")]
        public DateTime? PaidDate { get; set; }

        [Display(Name = "Recurring Monthly")]
        public bool IsRecurring { get; set; } = false;

        [MaxLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
