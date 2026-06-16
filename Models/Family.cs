namespace Hearthly.Models;

public class Family
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedByUserId { get; set; } = string.Empty;

    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
    public ICollection<FamilyInvitation> Invitations { get; set; } = new List<FamilyInvitation>();
}
