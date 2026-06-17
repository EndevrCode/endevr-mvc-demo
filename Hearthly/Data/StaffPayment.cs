using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Hearthly.Data
{
    public class StaffPayment
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StaffMemberId { get; set; }

        [ValidateNever]
        public StaffMember StaffMember { get; set; } = default!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Period Start")]
        public DateTime? PeriodStart { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Period End")]
        public DateTime? PeriodEnd { get; set; }

        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(500)]
        public string? Notes { get; set; }

        public string? RecordedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
