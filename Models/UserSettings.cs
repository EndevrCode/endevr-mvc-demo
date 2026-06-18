namespace Nestled.Models;

public class UserSettings
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;
    public FontSize FontSize { get; set; } = FontSize.Medium;

    public ApplicationUser? User { get; set; }
}
