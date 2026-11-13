using System.ComponentModel.DataAnnotations;

namespace Heimevernet.ViewModels.Account;

/// <summary>
/// Data for the 2FA setup page: the QR code to scan and the code the user types back.
/// </summary>
public class TwoFactorSetupViewModel
{
    /// <summary>The QR code as a base64-encoded PNG, shown with a <c>data:</c> URL.</summary>
    public string QrCodeImageBase64 { get; set; } = string.Empty;
    /// <summary>The secret as text, for users who cannot scan the QR code.</summary>
    public string ManualEntryKey { get; set; } = string.Empty;

    /// <summary>The 6-digit code from the authenticator app. Required, exactly 6 characters.</summary>
    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}