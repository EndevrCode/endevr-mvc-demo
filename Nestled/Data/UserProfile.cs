using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nestled.Data
{
    public class UserProfile
    {
        [Key]
        [ForeignKey(nameof(User))]
        public string UserId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [MaxLength(100)]
        [Display(Name = "Preferred Name")]
        public string PreferredName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "ID Number")]
        public string IdNumber { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Address")]
        public string Address { get; set; }

        // Relative path under wwwroot where the profile photo is stored, 
        // e.g. "/images/profiles/{userid}.jpg"
        public string PhotoPath { get; set; }

        // Navigation back to the user
        public ApplicationUser User { get; set; }

        /// <summary>
        /// Full URL or relative URL to the profile picture.
        /// Falls back to a default image if PhotoPath is null/empty.
        /// Not mapped to the database.
        /// </summary>
        [NotMapped]
        public string ProfilePictureUrl
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(PhotoPath))
                {
                    return PhotoPath;
                }
                // adjust default image location as needed
                return "/images/default-profile.png";
            }
        }
    }
}
