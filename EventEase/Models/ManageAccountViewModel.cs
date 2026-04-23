using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class ManageAccountViewModel
    {
        // Current state (read-only display)
        public string CurrentEmail { get; set; } = string.Empty;
        public string? CurrentDisplayName { get; set; }
        public string? AvatarBase64 { get; set; }

        // Profile section
        public UpdateProfileViewModel Profile { get; set; } = new();

        // Password section
        public ChangePasswordViewModel Password { get; set; } = new();
    }

    public class UpdateProfileViewModel
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters.")]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}