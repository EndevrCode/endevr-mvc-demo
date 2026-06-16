using System.ComponentModel.DataAnnotations;

namespace Hearthly.Data
{
    public class FamilyInvite
    {
        [Key]
        public Guid Token { get; set; }  // Exposed to admin to share

        [Required]
        public Guid FamilyId { get; set; }
        public Family Family { get; set; }

        [Required, EmailAddress]
        public string InvitedEmail { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
    }
}
