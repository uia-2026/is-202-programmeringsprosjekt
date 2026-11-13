using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Form data for the 2FA step during login.
/// </summary>
public class VerifyTwoFactorViewModel
{
    /// <summary>ID of the user who passed the password step. Sent back in a hidden field.</summary>
    public int UserId { get; set; }

    /// <summary>The 6-digit code from the authenticator app. Required, exactly 6 characters.</summary>
    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}