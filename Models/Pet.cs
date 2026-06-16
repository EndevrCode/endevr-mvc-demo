namespace Hearthly.Models;

public class Pet
{
    public int Id { get; set; }
    public int FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? PhotoPath { get; set; }
    public string? InsuranceProvider { get; set; }
    public string? MicrochipNumber { get; set; }
    public bool IsDeceased { get; set; } = false;
    public DateOnly? DeceasedDate { get; set; }
    public string? DeceasedNotes { get; set; }

    public Family? Family { get; set; }
    public ICollection<PetCareRecord> CareRecords { get; set; } = new List<PetCareRecord>();

    public int? AgeYears => BirthDate.HasValue
        ? (int)((DateOnly.FromDateTime(DateTime.Today) - BirthDate.Value).Days / 365.25)
        : null;
}
