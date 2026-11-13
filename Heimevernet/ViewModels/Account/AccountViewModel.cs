using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Data shown on the account page (Account/Index).
/// </summary>
public class AccountViewModel
{
    /// <summary>The user's display name.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>The user's email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The user's role.</summary>
    public UserRole Role { get; set; }
    /// <summary>Whether 2FA is turned on. Used to show or hide the "Set up 2FA" link.</summary>
    public bool TwoFactorEnabled { get; set; }
    /// <summary>Whether email notifications are on. Bound to the notification toggle.</summary>
    public bool EmailNotifications { get; set; }
}