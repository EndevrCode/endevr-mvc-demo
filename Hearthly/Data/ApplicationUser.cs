using Microsoft.AspNetCore.Identity;
using Hearthly.Data.RuleOfThree;
using System.Collections.Generic;

namespace Hearthly.Data
{
    public class ApplicationUser : IdentityUser
    {
        // Navigation properties
        public UserProfile Profile { get; set; }
        public ICollection<FamilyMember> FamilyMemberships { get; set; }
        public bool HasCompletedSetup { get; set; } = false;

        public bool AllowGuardianAccess { get; set; } = true;
        public DateTime? GuardianAccessDisabledAt { get; set; }

        public List<RuleOfThreeEntry> RuleOfThreeEntries { get; set; } = new();

        public byte[]? EncryptedVaultKey { get; set; }
        public string? VaultPinHash { get; set; }
        public byte[]? VaultKeySalt { get; set; }
    }
}
