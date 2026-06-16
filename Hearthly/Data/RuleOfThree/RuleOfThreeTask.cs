using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Hearthly.Data.RuleOfThree;
using System;

namespace Hearthly.Data.RuleOfThree
{
    public class RuleOfThreeTask
    {
        public Guid Id { get; set; }

        public Guid EntryId { get; set; }

        [ValidateNever]
        public RuleOfThreeEntry Entry { get; set; } = default!;

        public RuleOfThreeTaskType TaskType { get; set; } // Short or Maintenance
        public string Description { get; set; } = default!;
        public bool IsDone { get; set; } = false;

        public TimeSpan? Duration { get; set; } // Optional if timer was used
    }

    public enum RuleOfThreeTaskType
    {
        Short = 1,
        Maintenance = 2
    }
}
