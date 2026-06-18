using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Nestled.Data;
using System;
using System.Collections.Generic;

namespace Nestled.Data.RuleOfThree
{
    public class RuleOfThreeEntry
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = default!;
        [ValidateNever]
        public ApplicationUser User { get; set; } = default!;

        public Guid? FamilyId { get; set; } // nullable for individual use
        public bool IsFamilyEntry { get; set; }

        public DateTime Date { get; set; }

        public string? MainProject { get; set; }

        public List<RuleOfThreeTask> Tasks { get; set; } = new();

        public int UsedTimers { get; set; }
        public bool IsPowerDay { get; set; }

        public bool IsComplete { get; set; }
        public int StreakAtDay { get; set; }
    }
}
