using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Form data for the login page.
/// </summary>
public class LoginViewModel
{
    /// <summary>The user's email address. Required and must be a valid email address.</summary>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>The user's password. Required.</summary>
    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}