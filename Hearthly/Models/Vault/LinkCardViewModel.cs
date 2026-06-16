using Microsoft.AspNetCore.Http;
using Hearthly.Data.Vault;
using System.ComponentModel.DataAnnotations;

namespace Hearthly.Models.Vault
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
