using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data.Vault
{
    public class VaultPassword
    {
        [Key]
        public Guid Id { get; set; }

        public string? UserId { get; set; } = default!;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; } = default!;

        [Required]
        public VaultSection Section { get; set; }

        [Required]
        public PasswordType PasswordType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = default!;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [MaxLength(2048)]
        public string Password { get; set; } = default!;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? FilePath { get; set; }
    }

    public enum PasswordType
    {
        Website,
        App,
        Email,
        SocialMedia,
        Banking,
        WiFi,
        Device,
        SoftwareLicense,
        Other
    }

    public enum VaultSection
    {
        Passwords,
        BankAccounts,
        Documents,
        HousePlans,
        LegalDocs,
        Other
    }
}
