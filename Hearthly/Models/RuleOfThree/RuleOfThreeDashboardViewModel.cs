using System;
using System.Collections.Generic;
using Hearthly.Data.RuleOfThree;

namespace Hearthly.Models.RuleOfThree
{
    public class RuleOfThreeDashboardViewModel
    {
        public bool IsTodayComplete { get; set; }
        public List<RuleOfThreeEntry> ThisWeekEntries { get; set; } = new();
        public DateTime Today { get; set; }
        public int CurrentStreak { get; set; }
        public int DailyProgressPercent { get; set; }
    }
}
