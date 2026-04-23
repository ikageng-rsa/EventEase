using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    /// <summary>
    /// Extends ASP.NET Identity with app-specific profile data.
    /// One-to-one with IdentityUser via UserId (the Identity GUID).
    /// </summary>
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Display Name")]
        public string? DisplayName { get; set; }

        // Stored as a Base64 data URI — small avatar images only (< 200KB enforced in controller)
        public string? AvatarBase64 { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}