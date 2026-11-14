using Heimevernet.Models;
using Heimevernet.ViewModels.Account;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Handles registration, login, two-factor authentication, password and profile changes,
/// and sign-in/out for users.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates a new user account with a hashed password.
    /// </summary>
    /// <param name="model">The submitted registration form.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A <see cref="RegisterResult"/>; fails if the email is already in use.</returns>
    Task<RegisterResult> RegisterAsync(RegisterViewModel model, CancellationToken ct);

    /// <summary>Validates credentials. If the user has 2FA enabled, RequiresTwoFactor is true and no sign-in happens yet.</summary>
    /// <param name="model">The submitted login form (email and password).</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A <see cref="LoginResult"/> with the user when login succeeded, or an error message.</returns>
    Task<LoginResult> LoginAsync(LoginViewModel model, CancellationToken ct);

    /// <summary>Generates a new secret + QR code for a user setting up 2FA for the first time.</summary>
    /// <param name="userId">ID of the user setting up 2FA.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The secret and a base64 PNG QR code for an authenticator app.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user does not exist.</exception>
    Task<TwoFactorSetupViewModel> BeginTwoFactorSetupAsync(int userId, CancellationToken ct);

    /// <summary>Verifies a 2FA code against the user's already-stored secret (used at login time).</summary>
    /// <param name="userId">ID of the user who passed the password step.</param>
    /// <param name="code">The 6-digit code from the authenticator app.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A <see cref="TwoFactorVerifyResult"/> with the user when the code is valid.</returns>
    Task<TwoFactorVerifyResult> VerifyTwoFactorAsync(int userId, string code, CancellationToken ct);

    /// <summary>Confirms the code matches the pending secret, then saves and enables 2FA.</summary>
    /// <param name="userId">ID of the user enabling 2FA.</param>
    /// <param name="secret">The secret that was shown during setup.</param>
    /// <param name="code">The code the user typed from the authenticator app.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A <see cref="TwoFactorSetupResult"/> that tells if 2FA was enabled.</returns>
    Task<TwoFactorSetupResult> CompleteTwoFactorSetupAsync(int userId, string secret, string code, CancellationToken ct);

    /// <summary>Verifies the user's current password, updates it to the new one, rotates the security stamp, and records the change.</summary>
    /// <remarks>Rotating the security stamp signs the user out on other devices.</remarks>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="model">The submitted form with the current and new password.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A <see cref="ChangePasswordResult"/>; fails if the user does not exist or the current password is wrong.</returns>
    Task<ChangePasswordResult> ChangePasswordAsync(int userId, ChangePasswordViewModel model, CancellationToken ct);

    /// <summary>Updates a user's editable profile fields after trimming whitespace and rejecting duplicate email addresses and usernames.</summary>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="model">The submitted form with the new username and email.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An <see cref="AccountResult"/>; fails if the user does not exist or the email or username is taken by someone else.</returns>
    Task<AccountResult> UpdateProfileAsync(int userId, EditProfileViewModel model, CancellationToken ct);

    /// <summary>
    /// Signs the user in with a cookie that holds their ID, username, role and security stamp as claims.
    /// </summary>
    /// <param name="user">The user to sign in.</param>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A task that completes when the cookie has been issued.</returns>
    Task SignInAsync(User user, HttpContext httpContext);

    /// <summary>
    /// Signs the current user out by removing the authentication cookie.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    /// <returns>A task that completes when the user has been signed out.</returns>
    Task SignOutAsync(HttpContext httpContext);
}

/// <summary>
/// Outcome of <see cref="IAuthService.RegisterAsync"/>.
/// </summary>
/// <param name="Success"><see langword="true"/> when the account was created.</param>
/// <param name="Error">User-facing error message when registration failed.</param>
public record RegisterResult(
    bool Success,
    string? Error = null);

/// <summary>
/// Outcome of <see cref="IAuthService.LoginAsync"/>.
/// </summary>
/// <param name="Success"><see langword="true"/> when the password was correct.</param>
/// <param name="Error">User-facing error message when login failed.</param>
/// <param name="RequiresTwoFactor"><see langword="true"/> when the user must also enter a 2FA code before being signed in.</param>
/// <param name="UserId">ID of the user waiting for the 2FA step; only set when <paramref name="RequiresTwoFactor"/> is <see langword="true"/>.</param>
/// <param name="User">The user to sign in; only set when login is complete without 2FA.</param>
public record LoginResult(
    bool Success,
    string? Error = null,
    bool RequiresTwoFactor = false,
    int? UserId = null,
    User? User = null);

/// <summary>
/// Outcome of <see cref="IAuthService.VerifyTwoFactorAsync"/>.
/// </summary>
/// <param name="Success"><see langword="true"/> when the code was valid.</param>
/// <param name="Error">User-facing error message when verification failed.</param>
/// <param name="User">The user to sign in when verification succeeded.</param>
public record TwoFactorVerifyResult(
    bool Success,
    string? Error = null,
    User? User = null);

/// <summary>
/// Outcome of <see cref="IAuthService.CompleteTwoFactorSetupAsync"/>.
/// </summary>
/// <param name="Success"><see langword="true"/> when 2FA was enabled.</param>
/// <param name="Error">User-facing error message when setup failed.</param>
public record TwoFactorSetupResult(
    bool Success,
    string? Error = null);

/// <summary>
/// Outcome of <see cref="IAuthService.ChangePasswordAsync"/>.
/// </summary>
/// <param name="Success"><see langword="true"/> when the password was changed.</param>
/// <param name="Error">User-facing error message when the change failed.</param>
/// <param name="User">The updated user, so the caller can sign them in again with the new security stamp.</param>
public record ChangePasswordResult(
    bool Success,
    string? Error = null,
    User? User = null);

/// <summary>
/// Outcome of <see cref="IAuthService.UpdateProfileAsync"/>.
/// </summary>
/// <param name="Success"><see langword="true"/> when the profile was updated.</param>
/// <param name="Error">User-facing error message when the update failed.</param>
/// <param name="User">The updated user, so the caller can refresh the sign-in cookie with the new username.</param>
public record AccountResult(
    bool Success,
    string? Error = null,
    User? User = null);