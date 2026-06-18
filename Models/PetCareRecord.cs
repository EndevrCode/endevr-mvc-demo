namespace Nestled.Models;

public enum CareType { Grooming, Deworming, TickFlea, Checkup, Vaccination, Other }

public class PetCareRecord
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public CareType CareType { get; set; }
    public DateOnly Date { get; set; }
    public string? Notes { get; set; }
    public DateOnly? NextDueDate { get; set; }

    public Pet? Pet { get; set; }
}
