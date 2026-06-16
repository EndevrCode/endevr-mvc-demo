namespace Hearthly.Models;

public enum UtilityType { Electricity, Gas, Water, Internet, Other }

public class UtilityAccount
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public UtilityType UtilityType { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Family? Family { get; set; }
    public ICollection<UtilityPurchase> Purchases { get; set; } = new List<UtilityPurchase>();
}
