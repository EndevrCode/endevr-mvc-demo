using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Nestled.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data
{
    public class StaffMember
    {
        public Guid Id { get; set; }

        [ValidateNever]
        [Display(Name = "Family")]
        public Guid FamilyId { get; set; }
        [ValidateNever]
        public Family Family { get; set; } = default!;

        [Display(Name = "Photo")]
        public string? PhotoPath { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Preferred Name")]
        public string? PreferredName { get; set; }

        [Phone]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [Display(Name = "Nationality")]
        public string Nationality { get; set; } = "South Africa";

        [Display(Name = "ID Number")]
        public string? IdNumber { get; set; }

        [Display(Name = "Passport Number")]
        public string? PassportNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Work Days")]
        public string? WorkDays { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Daily Wage (ZAR)")]
        public decimal? DailyWageZAR { get; set; }

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [Display(Name = "Account Name")]
        public string? AccountName { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Account Number")]
        public string? AccountNumber { get; set; }

        [Display(Name = "Branch Code")]
        public string? BranchCode { get; set; }

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
