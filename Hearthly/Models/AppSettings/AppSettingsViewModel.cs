using System.ComponentModel.DataAnnotations;

namespace Hearthly.ViewModels
{
    public class AppSettingsViewModel
    {
        [Display(Name = "Enable Dark Mode")]
        public bool DarkMode { get; set; }
        public string ThemeMode { get; set; } = "system"; // Options: system, light, dark
        public string FontSize { get; set; } = "medium";  // Options: small, medium, large

        // Optional: add more settings below
        // [Display(Name = "Font Size")]
        // public string FontSize { get; set; }

        // [Display(Name = "Accent Color")]
        // public string AccentColor { get; set; }
    }
}
