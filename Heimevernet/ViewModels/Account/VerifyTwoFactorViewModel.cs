using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Account;

public class VerifyTwoFactorViewModel
{
    public int UserId { get; set; }

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}