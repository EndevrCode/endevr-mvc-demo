using System;
using System.ComponentModel.DataAnnotations;

namespace Hearthly.Data
{
    public class FamilyChore
    {
        public Guid Id { get; set; }

        public Guid FamilyId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? AssignedToUserId { get; set; }

        public bool IsDone { get; set; }

        public DateTime? DueDate { get; set; }

        public string CreatedByUserId { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public Family Family { get; set; } = null!;
        public ApplicationUser? AssignedTo { get; set; }
        public ApplicationUser CreatedBy { get; set; } = null!;
    }
}
