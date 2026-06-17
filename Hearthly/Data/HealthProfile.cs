using System;
using System.ComponentModel.DataAnnotations;

namespace Hearthly.Data
{
    public class HealthProfile
    {
        [Key]
        public string UserId { get; set; } = "";

        [MaxLength(10), Display(Name = "Blood Type")]
        public string? BloodType { get; set; }

        [Display(Name = "Allergies")]
        public string? Allergies { get; set; }

        [Display(Name = "Current Medications")]
        public string? CurrentMedications { get; set; }

        [Display(Name = "Vaccination Notes")]
        public string? VaccinationNotes { get; set; }

        [MaxLength(200), Display(Name = "Medical Aid Name")]
        public string? MedicalAidName { get; set; }

        [MaxLength(100), Display(Name = "Medical Aid Number")]
        public string? MedicalAidNumber { get; set; }

        [MaxLength(200), Display(Name = "Doctor / GP Name")]
        public string? DoctorName { get; set; }

        [MaxLength(50), Display(Name = "Doctor Phone")]
        public string? DoctorPhone { get; set; }

        [Display(Name = "Emergency Medical Notes")]
        public string? EmergencyNotes { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = null!;
    }
}
