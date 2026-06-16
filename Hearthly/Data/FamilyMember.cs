using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hearthly.Data
{
    public class FamilyMember
    {
        public Guid FamilyId { get; set; }
        public string UserId { get; set; }

        [Required]
        public string Role { get; set; }  // e.g. "Admin" or "Member"

        public bool IsAccepted { get; set; } = false;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation props
        public Family Family { get; set; }
        public ApplicationUser User { get; set; }
    }
}
