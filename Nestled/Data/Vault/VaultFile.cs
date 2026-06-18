using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data.Vault
{
    public class VaultFile
    {
        [Key]
        public Guid Id { get; set; }

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
