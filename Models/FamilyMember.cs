namespace Nestled.Models;

public enum FamilyRole { Admin, Member }

public class FamilyMember
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public FamilyRole Role { get; set; } = FamilyRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
    public ApplicationUser? User { get; set; }
}
