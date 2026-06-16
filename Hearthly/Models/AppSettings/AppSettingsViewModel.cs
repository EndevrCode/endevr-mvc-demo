using System.ComponentModel.DataAnnotations;

namespace Hearthly.ViewModels
{
    public class AppSettingsViewModel
    {
        public string ThemeMode { get; set; } = "system";
        public string FontSize { get; set; } = "medium";
    }
}
