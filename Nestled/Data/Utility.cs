using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data
{
    public class Utility
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Family")]
        public Guid FamilyId { get; set; }

        [ForeignKey(nameof(FamilyId))]
        public Family Family { get; set; } = default!;

        [Required]
        [Display(Name = "Utility Type")]
        public UtilityType Type { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Purchased From")]
        public string PurchasedFrom { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Account Used")]
        public string AccountUsed { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Purchase Date")]
        public DateTime PurchaseDate { get; set; }

        // Electricity‑only fields:

        [StringLength(100)]
        [Display(Name = "Token Number")]
        public string? TokenNumber { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Units (kWh)")]
        public decimal? TotalUnits { get; set; }
    }
}
