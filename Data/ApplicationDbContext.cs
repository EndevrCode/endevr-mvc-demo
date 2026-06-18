using Nestled.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Nestled.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Family> Families { get; set; }
    public DbSet<FamilyMember> FamilyMembers { get; set; }
    public DbSet<FamilyInvitation> FamilyInvitations { get; set; }
    public DbSet<Pet> Pets { get; set; }
    public DbSet<PetCareRecord> PetCareRecords { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<UtilityAccount> UtilityAccounts { get; set; }
    public DbSet<UtilityPurchase> UtilityPurchases { get; set; }
    public DbSet<ServiceContact> ServiceContacts { get; set; }
    public DbSet<PasswordVaultEntry> PasswordVaultEntries { get; set; }
    public DbSet<BankAccount> BankAccounts { get; set; }
    public DbSet<VaultDocument> VaultDocuments { get; set; }
    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingItem> ShoppingItems { get; set; }
    public DbSet<Bill> Bills { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FamilyMember>()
            .HasOne(m => m.Family)
            .WithMany(f => f.Members)
            .HasForeignKey(m => m.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FamilyMember>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Family>()
            .HasOne(f => f.CreatedByUser)
            .WithMany()
            .HasForeignKey(f => f.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FamilyInvitation>()
            .HasOne(i => i.Family)
            .WithMany(f => f.Invitations)
            .HasForeignKey(i => i.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Pet>()
            .HasOne(p => p.Family)
            .WithMany()
            .HasForeignKey(p => p.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PetCareRecord>()
            .HasOne(r => r.Pet)
            .WithMany(p => p.CareRecords)
            .HasForeignKey(r => r.PetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UtilityPurchase>()
            .HasOne(p => p.UtilityAccount)
            .WithMany(a => a.Purchases)
            .HasForeignKey(p => p.UtilityAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ShoppingItem>()
            .HasOne(i => i.ShoppingList)
            .WithMany(l => l.Items)
            .HasForeignKey(i => i.ShoppingListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserSettings>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Bill>()
            .Property(b => b.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<UtilityPurchase>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<Staff>()
            .Property(s => s.DailyWage)
            .HasColumnType("decimal(18,2)");
    }
}
