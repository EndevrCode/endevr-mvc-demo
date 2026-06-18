using Microsoft.AspNetCore.Http;
using Nestled.Data.Vault;
using System.ComponentModel.DataAnnotations;

namespace Nestled.Models.Vault
{
    public class LinkCardViewModel
    {
        public int AccountId { get; set; }

        public string? AccountHolder { get; set; }

        public string? BankName { get; set; }

        public string? LastFourDigits { get; set; }

        [Display(Name = "Front of Card")]
        public IFormFile? CardFront { get; set; }

        [Display(Name = "Back of Card")]
        public IFormFile? CardBack { get; set; }
    }
}
