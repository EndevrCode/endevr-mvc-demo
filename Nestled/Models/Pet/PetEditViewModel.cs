using Microsoft.AspNetCore.Http;
using Nestled.Data;
using System;
using System.ComponentModel.DataAnnotations;

namespace Nestled.ViewModels
{
    public class PetEditViewModel
    {
        public Pet Pet { get; set; } = new Pet();

        [Display(Name = "Pet Photo")]
        public IFormFile? PhotoFile { get; set; }

        public string? PhotoPath { get; set; }

        // Used for dropdown
        public Guid FamilyId
        {
            get => Pet.FamilyId;
            set => Pet.FamilyId = value;
        }

        // Optional decimal parsing field
        public decimal? LastWeightKg
        {
            get => Pet.LastWeightKg;
            set => Pet.LastWeightKg = value ?? 0;
        }

        public DateTime? LastWeighedDate
        {
            get => Pet.LastWeighedDate;
            set => Pet.LastWeighedDate = value;
        }

        public DateTime? LastDewormingDate
        {
            get => Pet.LastDewormingDate;
            set => Pet.LastDewormingDate = value;
        }

        public DateTime? LastTickFleaDate
        {
            get => Pet.LastTickFleaDate;
            set => Pet.LastTickFleaDate = value;
        }

        public DateTime? LastGroomingDate
        {
            get => Pet.LastGroomingDate;
            set => Pet.LastGroomingDate = value;
        }

        public DateTime? LastCheckupDate
        {
            get => Pet.LastCheckupDate;
            set => Pet.LastCheckupDate = value;
        }

        public bool HasInsurance
        {
            get => Pet.HasInsurance;
            set => Pet.HasInsurance = value;
        }

        public string? InsuranceNumber
        {
            get => Pet.InsuranceNumber;
            set => Pet.InsuranceNumber = value;
        }

        public bool IsMicrochipped
        {
            get => Pet.IsMicrochipped;
            set => Pet.IsMicrochipped = value;
        }

        public string? MicrochipNumber
        {
            get => Pet.MicrochipNumber;
            set => Pet.MicrochipNumber = value;
        }

        public bool IsDeceased
        {
            get => Pet.IsDeceased;
            set => Pet.IsDeceased = value;
        }

        public DateTime? DateOfDeath
        {
            get => Pet.DateOfDeath;
            set => Pet.DateOfDeath = value;
        }
    }
}
