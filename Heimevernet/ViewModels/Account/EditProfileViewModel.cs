using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

public class EditProfileViewModel
{
    [Required]
    [StringLength(FieldLimits.Username)]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(FieldLimits.Email)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
