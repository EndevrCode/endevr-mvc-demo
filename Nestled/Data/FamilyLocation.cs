using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data
{
    public class FamilyLocation
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public Guid FamilyId { get; set; }

        [ForeignKey("FamilyId")]
        public Family? Family { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? Note { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsVisible { get; set; } = true;

        public int? BatteryLevel { get; set; }
        public bool? IsCharging { get; set; }
        public double? Speed { get; set; }

        [StringLength(100)]
        public string? PlaceName { get; set; }
    }
}
