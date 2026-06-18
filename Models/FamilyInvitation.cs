namespace Nestled.Models;

public enum InvitationStatus { Pending, Accepted, Revoked }

public class FamilyInvitation
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public Guid Token { get; set; } = Guid.NewGuid();
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public Family? Family { get; set; }
}
