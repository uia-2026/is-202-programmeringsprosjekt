using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Form data for changing the signed-in user's username and email.
/// </summary>
public class EditProfileViewModel
{
    /// <summary>The new username. Required; at most <see cref="FieldLimits.Username"/> characters. Must not be used by another user.</summary>
    [Required]
    [StringLength(FieldLimits.Username)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>The new email address. Required, must be a valid email; at most <see cref="FieldLimits.Email"/> characters. Must not be used by another user.</summary>
    [Required]
    [EmailAddress]
    [StringLength(FieldLimits.Email)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}