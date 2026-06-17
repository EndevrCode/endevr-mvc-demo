using System;
using System.ComponentModel.DataAnnotations;

namespace Hearthly.Data
{
    public class FamilyCalendarEvent
    {
        public Guid Id { get; set; }

        public Guid FamilyId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime Date { get; set; }

        [MaxLength(20)]
        public string Color { get; set; } = "#6366f1";

        public string CreatedByUserId { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Family Family { get; set; } = null!;
        public ApplicationUser CreatedBy { get; set; } = null!;
    }
}
