using Hearthly.Data;
using System.ComponentModel.DataAnnotations;

public enum EmergencyContactType
{
    Police,
    Fire,
    Ambulance,
    Doctor,
    PoisonControl,
    NSRI,
    TowingService,
    Mom,
    Dad,
    Brother,
    Sister,
    NextOfKin,
    Other
}

public class EmergencyContact
{
    public int Id { get; set; }

    // Null => global, otherwise family‑specific
    public Guid? FamilyId { get; set; }

    [Required]
    [Display(Name = "Type")]
    public EmergencyContactType ContactType { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = default!;

    [Required, Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = default!;

    public string? Notes { get; set; }

    // navigation
    public Family? Family { get; set; }
}
