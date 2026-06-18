using Microsoft.AspNetCore.Identity;
using Nestled.Data;
using Nestled.Models;

namespace Nestled.Models
{
    public class ProfileViewModel
    {
        // the ASP‑NET Identity user
        public ApplicationUser User { get; set; } = default!;
        // your extended profile info
        public UserProfile Profile { get; set; } = default!;
    }
}
