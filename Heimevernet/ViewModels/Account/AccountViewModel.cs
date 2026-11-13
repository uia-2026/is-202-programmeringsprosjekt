using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;


namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Data for the account page: profile fields, password change fields and current settings.
/// </summary>
public class AccountViewModel
{
    /// <summary>The username. Required; at most <see cref="FieldLimits.Username"/> characters.</summary>
    [Required, StringLength(FieldLimits.Username)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>The email address. Required, must be a valid email; at most <see cref="FieldLimits.Email"/> characters.</summary>
    [Required, EmailAddress, StringLength(FieldLimits.Email)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>The current password, for the password change form. Required; at most <see cref="FieldLimits.PasswordHash"/> characters.</summary>
    [Required, DataType(DataType.Password), StringLength(FieldLimits.PasswordHash)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The new password. Required; at most <see cref="FieldLimits.PasswordHash"/> characters.</summary>
    [Required, DataType(DataType.Password), StringLength(FieldLimits.PasswordHash)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>The new password typed again. Required; must match <see cref="NewPassword"/>.</summary>
    [Required, DataType(DataType.Password), Compare(nameof(NewPassword)), StringLength(FieldLimits.PasswordHash)]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    /// <summary><see langword="true"/> when the profile edit form should be shown.</summary>
    public bool EditProfile { get; set; }

    /// <summary><see langword="true"/> when the password change form should be shown.</summary>
    public bool ChangePassword { get; set; }

    /// <summary>Name of the section on the page that should be open, for example after a form error.</summary>
    public string OpenSection { get; set; } = string.Empty;

    /// <summary>The user's role.</summary>
    public UserRole Role { get; set; }

    /// <summary><see langword="true"/> when two-factor authentication is on.</summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary><see langword="true"/> when the user wants email notifications.</summary>
    public bool EmailNotifications { get; set; }
}