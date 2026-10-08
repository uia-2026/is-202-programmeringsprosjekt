using Heimevernet.Models;
using Heimevernet.ViewModels.Account;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Handles registration, login, two-factor authentication, and sign-in/out for users.
/// </summary>
public interface IAuthService
{
    Task<RegisterResult> RegisterAsync(RegisterViewModel model, CancellationToken ct);

    /// <summary>Validates credentials. If the user has 2FA enabled, RequiresTwoFactor is true and no sign-in happens yet.</summary>
    Task<LoginResult> LoginAsync(LoginViewModel model, CancellationToken ct);

    /// <summary>Generates a new secret + QR code for a user setting up 2FA for the first time.</summary>
    Task<TwoFactorSetupViewModel> BeginTwoFactorSetupAsync(int userId, CancellationToken ct);

    /// <summary>Verifies a 2FA code against the user's already-stored secret (used at login time).</summary>
    Task<TwoFactorVerifyResult> VerifyTwoFactorAsync(int userId, string code, CancellationToken ct);

    /// <summary>Confirms the code matches the pending secret, then saves and enables 2FA.</summary>
    Task<TwoFactorSetupResult> CompleteTwoFactorSetupAsync(int userId, string secret, string code, CancellationToken ct);

    Task SignInAsync(User user, HttpContext httpContext);

    Task SignOutAsync(HttpContext httpContext);
}


public record RegisterResult(
    bool Success,
    string? Error = null);

public record LoginResult(
    bool Success,
    string? Error = null,
    bool RequiresTwoFactor = false,
    int? UserId = null,
    User? User = null);

public record TwoFactorVerifyResult(
    bool Success,
    string? Error = null,
    User? User = null);

public record TwoFactorSetupResult(
    bool Success,
    string? Error = null);