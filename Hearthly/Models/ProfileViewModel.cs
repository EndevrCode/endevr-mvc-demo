using Microsoft.AspNetCore.Identity;
using Hearthly.Data;
using Hearthly.Models;

namespace Hearthly.Models
{
    public class ProfileViewModel
    {
        // the ASP‑NET Identity user
        public ApplicationUser User { get; set; } = default!;
        // your extended profile info
        public UserProfile Profile { get; set; } = default!;
    }
}
