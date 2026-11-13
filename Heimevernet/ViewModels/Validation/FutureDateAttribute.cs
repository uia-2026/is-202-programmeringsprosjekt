using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Validation;

/// <summary>
/// Validation attribute that ensures a nullable DateTime (if provided) is in the future (UTC).
/// </summary>
public sealed class FutureDateAttribute : ValidationAttribute
{
    /// <summary>
    /// Returns success for <see langword="null"/> or a date after the current UTC time, and an error otherwise.
    /// </summary>
    /// <param name="value">The value to check. Should be a <see cref="DateTime"/> or <see langword="null"/>.</param>
    /// <param name="validationContext">The validation context (not used).</param>
    /// <returns><see cref="ValidationResult.Success"/>, or a <see cref="ValidationResult"/> with the error message.</returns>
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