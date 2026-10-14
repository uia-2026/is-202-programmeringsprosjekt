using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Validation;

/// <summary>
/// Validation attribute that ensures a nullable DateTime (if provided) is in the future (UTC).
/// </summary>
public sealed class FutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;

        if (value is DateTime dt)
        {
            if (dt.ToUniversalTime() <= DateTime.UtcNow)
            {
                return new ValidationResult(ErrorMessage ?? "Date and time must be in the future.");
            }
            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid date value.");
    }
}
