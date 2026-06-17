using System;
using System.Collections.Generic;

namespace Hearthly.Data
{
    public class DashboardViewModel
    {
        public List<FamilyInfo> Families { get; set; } = new();
        public List<FamilyInvite> RecentInvites { get; set; } = new();
        public List<UserProfile> MemberProfiles { get; set; } = new();

        // Pending invites for the current user
        public List<FamilyInvite> PendingInvites { get; set; } = new();

        // Bills summary
        public int UnpaidBillsCount { get; set; }
        public decimal UnpaidBillsTotal { get; set; }
        public List<Bill> OverdueBills { get; set; } = new();
        public List<Bill> UpcomingBills { get; set; } = new();

        // Upcoming birthdays (next 30 days)
        public List<BirthdayInfo> UpcomingBirthdays { get; set; } = new();

        // Pending chores across all families
        public int PendingChoresCount { get; set; }

        // Pet care reminders
        public List<PetCareReminder> PetCareReminders { get; set; } = new();

        // FullCalendar JSON blob of events (birthdays, invites, etc.)
        public string EventsJson { get; set; } = "[]";

        // Families available for event creation (id + name)
        public string FamiliesJson { get; set; } = "[]";
    }

    public class PetCareReminder
    {
        public Guid PetId { get; set; }
        public string PetName { get; set; } = "";
        public string CareType { get; set; } = "";
        public DateTime LastDate { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class BirthdayInfo
    {
        public string Name { get; set; } = "";
        public DateTime NextBirthday { get; set; }
        public int DaysUntil { get; set; }
        public bool IsPet { get; set; }
    }

    public class FamilyInfo
    {
        public Family Family { get; set; } = default!;
        public List<MemberInfo> Members { get; set; } = new();
        public List<Pet> Pets { get; set; } = new();
    }

    public class MemberInfo
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PreferredName { get; set; }
        public string Role { get; set; } = "";
        public bool IsAccepted { get; set; }
        public string? PhotoPath { get; set; }
    }
}
