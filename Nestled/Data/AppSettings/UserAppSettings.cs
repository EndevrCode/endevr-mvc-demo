using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data
{
    public class UserAppSettings
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        public string ThemeMode { get; set; } = "system"; // "light", "dark", or "system"
        public string FontSize { get; set; } = "medium";   // "small", "medium", "large"
    }
}
