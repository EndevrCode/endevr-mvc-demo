using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hearthly.Data
{
    public enum FamilyPlaceType { Home = 0, Work = 1, School = 2, Custom = 3 }

    public class FamilyPlace
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid FamilyId { get; set; }

        [ForeignKey("FamilyId")]
        public Family? Family { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        public FamilyPlaceType PlaceType { get; set; } = FamilyPlaceType.Custom;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int RadiusMeters { get; set; } = 200;

        [Required]
        public string CreatedByUserId { get; set; } = default!;

        [ForeignKey("CreatedByUserId")]
        public ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
