using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Hearthly.Data.RuleOfThree;
using Hearthly.Data.Shopping;
using Hearthly.Data.Vault;


namespace Hearthly.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Family> Families { get; set; }
        public DbSet<FamilyMember> FamilyMembers { get; set; }
        public DbSet<FamilyInvite> FamilyInvites { get; set; }
        public DbSet<Pet> Pets { get; set; }
        public DbSet<Utility> Utilities { get; set; }
        public DbSet<StaffMember> StaffMembers { get; set; } = default!;
        public DbSet<ServiceContact> ServiceContacts { get; set; } = default!;
        public DbSet<EmergencyContact> EmergencyContacts { get; set; } = default!;
        public DbSet<RuleOfThreeEntry> RuleOfThreeEntries { get; set; }
        public DbSet<RuleOfThreeTask> RuleOfThreeTasks { get; set; }
        public DbSet<UserAppSettings> UserAppSettings { get; set; }
        public DbSet<Hearthly.Data.Vault.VaultFile> VaultFiles { get; set; } = default!;
        public DbSet<VaultPassword> VaultPasswords { get; set; }
        public DbSet<VaultBankAccount> VaultBankAccounts { get; set; }
        public DbSet<Bill> Bills { get; set; } = default!;
        public DbSet<ShoppingList> ShoppingLists { get; set; } = default!;
        public DbSet<ShoppingItem> ShoppingItems { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure composite PK for FamilyMember, non‑clustered
            builder.Entity<FamilyMember>(entity =>
            {
                entity.HasKey(fm => new { fm.FamilyId, fm.UserId })
                      .IsClustered(false);

                entity.HasOne(fm => fm.Family)
                      .WithMany(f => f.Members)
                      .HasForeignKey(fm => fm.FamilyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fm => fm.User)
                      .WithMany(u => u.FamilyMemberships)
                      .HasForeignKey(fm => fm.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Emergency contacts are seeded via EF migrations (InsertData)
        }
    }
}

