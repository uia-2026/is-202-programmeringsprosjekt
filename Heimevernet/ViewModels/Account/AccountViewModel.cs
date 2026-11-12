using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

public class AccountViewModel
{
    [Required, StringLength(FieldLimits.Username)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(FieldLimits.Email)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(FieldLimits.PasswordHash)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(FieldLimits.PasswordHash)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword)), StringLength(FieldLimits.PasswordHash)]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;

    public bool EditProfile { get; set; }
    public bool ChangePassword { get; set; }
    public string OpenSection { get; set; } = string.Empty;

    public UserRole Role { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool EmailNotifications { get; set; }
}