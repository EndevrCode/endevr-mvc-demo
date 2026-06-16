using System.ComponentModel.DataAnnotations;

namespace Hearthly.Data
{
    public class Family
    {
        [Key]
        public Guid Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public string CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FamilyMember> Members { get; set; }
    }
}
