namespace Hearthly.Models;

public class UtilityPurchase
{
    public int Id { get; set; }
    public int UtilityAccountId { get; set; }
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public decimal? Units { get; set; }
    public string? Token { get; set; }
    public string? Notes { get; set; }

    public UtilityAccount? UtilityAccount { get; set; }
}
