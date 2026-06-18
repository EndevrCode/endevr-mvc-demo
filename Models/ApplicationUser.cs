using Microsoft.AspNetCore.Identity;

namespace Nestled.Models;

public enum ThemeMode { System, Light, Dark }
public enum FontSize { Small, Medium, Large }

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? IdNumber { get; set; }
    public new string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? PhotoPath { get; set; }
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;
    public FontSize FontSize { get; set; } = FontSize.Medium;
    public string? VaultPin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string DisplayName => PreferredName ?? FirstName;
    public string FullName => $"{FirstName} {LastName}";
}
