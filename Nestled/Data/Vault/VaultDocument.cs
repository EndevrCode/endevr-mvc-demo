using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data.Vault
{
    public enum DocumentCategory
    {
        Identity,
        Insurance,
        Medical,
        Legal,
        Contract,
        Financial,
        Property,
        Other
    }

    public class VaultDocument
    {
        [Key]
        public Guid Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        public DocumentCategory Category { get; set; } = DocumentCategory.Other;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        public string OriginalFileName { get; set; } = default!;

        [Required]
        public string ContentType { get; set; } = default!;

        [Required]
        public byte[] EncryptedData { get; set; } = default!;

        [Required]
        public string UserId { get; set; } = default!;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
