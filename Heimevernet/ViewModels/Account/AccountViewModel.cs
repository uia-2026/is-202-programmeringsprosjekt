using Heimevernet.Models;

namespace Heimevernet.ViewModels.Account;

public class AccountViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool TwoFactorEnabled { get; set; }
}