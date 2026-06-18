namespace Nestled.Models;

public class Staff
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Nationality { get; set; }
    public string? IdNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? WorkDays { get; set; }
    public decimal DailyWage { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountType { get; set; }
    public string? Address { get; set; }
    public string? PhotoPath { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Family? Family { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
