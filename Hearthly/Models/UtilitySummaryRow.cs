using System;

namespace Hearthly.Models
{
    public class UtilitySummaryRow
    {
        public DateTime PurchaseDate { get; set; }
        public decimal? UnitsReceived { get; set; }
        public decimal AmountPaid { get; set; }
        public string PurchasedFrom { get; set; } = string.Empty;
        public DateTime? PreviousPurchase { get; set; }
        public int? DaysBetween { get; set; }
        public decimal? AverageDailyUsage { get; set; }
    }
}
