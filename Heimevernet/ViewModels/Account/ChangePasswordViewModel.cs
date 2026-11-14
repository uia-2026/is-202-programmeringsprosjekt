using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Form data for changing the signed-in user's password.
/// </summary>
public class ChangePasswordViewModel
{
    /// <summary>The current password. Required.</summary>
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The new password. Required; between 8 and <see cref="FieldLimits.PasswordHash"/> characters.</summary>
    [Required]
    [DataType(DataType.Password)]
    [StringLength(FieldLimits.PasswordHash, MinimumLength = 8, ErrorMessage = "The new password must be at least 8 characters long.")]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>The new password typed again. Required; must match <see cref="NewPassword"/>.</summary>
    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation password do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}