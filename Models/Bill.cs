namespace Hearthly.Models;

public class Bill
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public bool IsPaid { get; set; } = false;
    public DateOnly? PaidDate { get; set; }
    public bool RecurringMonthly { get; set; } = false;
    public string? Notes { get; set; }

    public Family? Family { get; set; }

    public bool IsOverdue => !IsPaid && DueDate < DateOnly.FromDateTime(DateTime.Today);
    public bool IsDueSoon => !IsPaid && DueDate >= DateOnly.FromDateTime(DateTime.Today)
                             && DueDate <= DateOnly.FromDateTime(DateTime.Today.AddDays(7));
}
