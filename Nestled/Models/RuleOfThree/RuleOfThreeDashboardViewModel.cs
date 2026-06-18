using System;
using System.Collections.Generic;
using Nestled.Data.RuleOfThree;

namespace Nestled.Models.RuleOfThree
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
