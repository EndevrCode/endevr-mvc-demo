using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Hearthly.Data
{
    public enum ServiceType
    {
        Electrician,

        Plumber,

        Lawyer,

        [Display(Name = "Family Doctor")]
        Doctor,

        [Display(Name = "Handy Man")]
        Handyman,

        [Display(Name = "Medical Aid Broker")]
        MedicalAid,

        [Display(Name = "Cellphone Network Provider")]
        Cellphone,

        [Display(Name = "Insurance Broker")]
        Insurance,

        [Display(Name = "Cleaning Services")]
        Cleaner,

        Dentist,

        Therapist,

        Builder,

        [Display(Name ="Internet Service Provider")]
        ISP,

        [Display(Name = "Towing Services")]
        Towing,

        Mechanic,

        Painter,

        Solar,

        Other
    }

    public class ServiceContact
    {
        public int Id { get; set; }

        [Required]
        [ScaffoldColumn(false)]
        public Guid FamilyId { get; set; }           // linked behind the scenes

        [Required]
        [Display(Name = "Service Type")]
        public ServiceType ServiceType { get; set; } // dropdown

        [Required, StringLength(100)]
        [Display(Name = "Company")]
        public string Company { get; set; } = default!;

        [Required, StringLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; } = default!;

        [Required, Phone]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = default!;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
        [ValidateNever]
        [ScaffoldColumn(false)]
        public Family Family { get; set; } = default!;
    }
}
