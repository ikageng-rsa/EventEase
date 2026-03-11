using System.ComponentModel.DataAnnotations;

namespace EventEase.Validation
{
    /// <summary>
    /// Validates that a date is today or in the future.
    /// Used to prevent events from being created with a past date.
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
        {
            ErrorMessage = "Event date must be today or in the future.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                if (date.Date < DateTime.Today)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}