namespace Hearthly.Models;

public enum DocumentCategory { Personal, Legal, HousePlans, Medical, Financial, Other }

public class VaultDocument
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DocumentCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
    public ApplicationUser? User { get; set; }

    public string FileSizeDisplay => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        _ => $"{FileSize / (1024.0 * 1024):F1} MB"
    };
}
