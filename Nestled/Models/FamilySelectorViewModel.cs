using System;
using System.Collections.Generic;
using Nestled.Data;

namespace Nestled.Models
{
    public class FamilySelectorViewModel
    {
        public IEnumerable<Family> Families { get; set; } = Array.Empty<Family>();
        public Guid? SelectedFamilyId { get; set; }
    }
}
