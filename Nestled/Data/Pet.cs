using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data
{
    public class Pet
    {
        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Pet Name")]
        public string Name { get; set; } = default!;

        [StringLength(50)]
        [Display(Name = "Species")]
        public string Species { get; set; } = default!;

        [StringLength(100)]
        [Display(Name = "Breed")]
        public string Breed { get; set; } = default!;

        [StringLength(200)]
        [Display(Name = "Profile Photo Path")]
        public string? PhotoPath { get; set; }

        // Family relationship
        [ForeignKey(nameof(Family))]
        [Required]
        [Display(Name = "Family")]
        public Guid FamilyId { get; set; }

        [ValidateNever]
        [Display(Name = "Family")]
        public Family Family { get; set; } = default!;

        // Insurance
        [Display(Name = "Has Insurance?")]
        public bool HasInsurance { get; set; }

        [StringLength(50)]
        [Display(Name = "Insurance Number")]
        public string? InsuranceNumber { get; set; }

        // Vet info
        [StringLength(100)]
        [Display(Name = "Veterinarian Name")]
        public string? VetName { get; set; }

        [StringLength(50)]
        [Display(Name = "Veterinarian Contact")]
        public string? VetContact { get; set; }

        // Microchip
        [Display(Name = "Microchipped?")]
        public bool IsMicrochipped { get; set; }

        [StringLength(50)]
        [Display(Name = "Microchip Number")]
        public string? MicrochipNumber { get; set; }

        // Birth & age
        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { get; set; }

        // Service dates
        [DataType(DataType.Date)]
        [Display(Name = "Last Deworming Date")]
        public DateTime? LastDewormingDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Tick & Flea Date")]
        public DateTime? LastTickFleaDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last Grooming Date")]
        public DateTime? LastGroomingDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Last General Checkup Date")]
        public DateTime? LastCheckupDate { get; set; }

        // Deceased flag and death date
        [Display(Name = "Is Deceased?")]
        public bool IsDeceased { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Death")]
        public DateTime? DateOfDeath { get; set; }

        // Computed property—won't be mapped to the DB
        [NotMapped]
        [Display(Name = "Final Age at Death")]
        public string? FinalAge
        {
            get
            {
                if (!BirthDate.HasValue || !DateOfDeath.HasValue)
                    return null;

                var span = DateOfDeath.Value - BirthDate.Value;
                int years = (int)(span.Days / 365.25);
                int months = (int)((span.Days % 365.25) / 30);
                return $"{years} yr{(years == 1 ? "" : "s")} {months} mo{(months == 1 ? "" : "s")}";
            }
        }

        [Display(Name = "Last Known Weight")]
        [Range(0, 1000, ErrorMessage = "Enter a valid weight")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal? LastWeightKg { get; set; }

        [Display(Name = "When last weight was taken?")]
        [DataType(DataType.Date)]
        public DateTime? LastWeighedDate { get; set; }
    }
}
