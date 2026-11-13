using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Form data for the registration page.
/// </summary>
public class RegisterViewModel
{
    /// <summary>Display name. Required, max <see cref="FieldLimits.Username"/> characters.</summary>
    [Required, StringLength(FieldLimits.Username)]
    public string Username { get; set; } = string.Empty;

    /// <summary>Email address used to log in. Required, valid format, max <see cref="FieldLimits.Email"/> characters, and must not already be in use.</summary>
    [Required, EmailAddress, StringLength(FieldLimits.Email)]
    public string Email { get; set; } = string.Empty;

    /// <summary>The chosen password. Required, max <see cref="FieldLimits.PasswordHash"/> characters.</summary>
    [Required, DataType(DataType.Password), StringLength(FieldLimits.PasswordHash)]
    public string Password { get; set; } = string.Empty;

    /// <summary>The password typed again. Must be the same as <see cref="Password"/>.</summary>
    [Required, DataType(DataType.Password), Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>The role the user signs up as. Required.</summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>What kind of actor the user is, e.g. "Municipality". Required, max <see cref="FieldLimits.ActorType"/> characters.</summary>
    [Required, StringLength(FieldLimits.ActorType)]
    public string ActorType { get; set; } = string.Empty;
}