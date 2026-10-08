using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Account;

public class TwoFactorSetupViewModel
{
    public string QrCodeImageBase64 { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}