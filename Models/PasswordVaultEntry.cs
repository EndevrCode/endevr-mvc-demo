namespace Nestled.Models;

public class PasswordVaultEntry
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string EncryptedPassword { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
    public ApplicationUser? User { get; set; }
}
