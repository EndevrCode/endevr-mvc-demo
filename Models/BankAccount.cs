namespace Nestled.Models;

public class BankAccount
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string AccountHolder { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? CardFrontImagePath { get; set; }
    public string? CardBackImagePath { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }
}
