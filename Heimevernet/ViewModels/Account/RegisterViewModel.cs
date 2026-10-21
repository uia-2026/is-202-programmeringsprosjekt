using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

public class RegisterViewModel
{
    [Required, StringLength(FieldLimits.Username)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(FieldLimits.Email)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(FieldLimits.PasswordHash)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    [Required, StringLength(FieldLimits.ActorType)]
    public string ActorType { get; set; } = string.Empty;
}