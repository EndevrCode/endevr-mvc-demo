using Microsoft.AspNetCore.Http;
using Nestled.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace Nestled.ViewModels
{
    public class PetCreateViewModel
    {
        public PetCreateViewModel()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Species { get; set; }
        public string? Breed { get; set; }

        [Required]
        public Guid FamilyId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        public decimal? LastWeightKg { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastWeighedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastDewormingDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastTickFleaDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastGroomingDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LastCheckupDate { get; set; }

        public bool HasInsurance { get; set; }
        public string? InsuranceNumber { get; set; }

        public bool IsMicrochipped { get; set; }
        public string? MicrochipNumber { get; set; }

        public bool IsDeceased { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfDeath { get; set; }

        [Required]
        public IFormFile PhotoFile { get; set; }
        public Pet Pet { get; set; }
    }
}
