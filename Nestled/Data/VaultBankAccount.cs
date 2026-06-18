using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data.Vault
{
    public class VaultBankAccount
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [Display(Name = "Account Holder")]
        public string AccountHolder { get; set; }

        [Required]
        [Display(Name = "Bank Name")]
        public BankName BankName { get; set; }

        [Required]
        [Display(Name = "Account Type")]
        public AccountType AccountType { get; set; }

        [Required]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        [Display(Name = "Branch Code")]
        public string? BranchCode { get; set; }

        [Display(Name = "Linked To Me")]
        public bool IsMine { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for card image
        public string? CardFrontPath { get; set; }
        public string? CardBackPath { get; set; }
    }

    public enum BankName
    {
        Absa,
        Capitec,
        Discovery,
        FNB,
        Investec,
        Nedbank,
        StandardBank,
        TymeBank,
        Other
    }

    public enum AccountType
    {
        Cheque,
        Savings,
        Business,
        Credit,
        Transmission,
        Other
    }
}
