using Heimevernet.Extensions;
using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for accounts: registration, login with optional two-factor authentication,
/// account settings (profile, password, email notifications) and logout.
/// </summary>
/// <remarks>
/// The app has a fallback policy that requires a signed-in user, so actions without
/// <see cref="AllowAnonymousAttribute"/> need login even when they have no <see cref="AuthorizeAttribute"/>.
/// </remarks>
/// <param name="authService">Handles registration, login, 2FA, password and profile changes.</param>
/// <param name="userService">Reads and updates account settings.</param>
public class AccountController(IAuthService authService, IUserService userService) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Shows the account page for the signed-in user.
    /// </summary>
    /// <remarks><c>GET /Account</c>. Requires a signed-in user.</remarks>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The account view, or 404 Not Found if the user no longer exists.</returns>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await _userService.GetAccountAsync(User.GetUserId(), ct);

        if (user == null)
            return NotFound();

        return View(user);
    }

    /// <summary>
    /// Shows the registration form.
    /// </summary>
    /// <remarks><c>GET /Account/Register</c>. Open to anonymous users.</remarks>
    /// <returns>The registration view with an empty form.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    /// <summary>
    /// Creates a new account from the submitted form.
    /// </summary>
    /// <remarks><c>POST /Account/Register</c>. Open to anonymous users. Requires an anti-forgery token.</remarks>
    /// <param name="model">The submitted registration form.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A redirect to the login page on success; otherwise the form again with errors.</returns>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _authService.RegisterAsync(model, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Account created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Shows the login form.
    /// </summary>
    /// <remarks><c>GET /Account/Login</c>. Open to anonymous users.</remarks>
    /// <returns>The login view with an empty form.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    /// <summary>
    /// Checks the email and password. Signs the user in, or sends them to the 2FA step if 2FA is on.
    /// </summary>
    /// <remarks><c>POST /Account/Login</c>. Open to anonymous users. Requires an anti-forgery token.</remarks>
    /// <param name="model">The submitted login form.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to the home page, a redirect to <see cref="VerifyTwoFactor()"/> when 2FA is needed,
    /// or the form again with errors.
    /// </returns>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _authService.LoginAsync(model, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        if (result.RequiresTwoFactor)
        {
            TempData["PendingUserId"] = result.UserId;
            return RedirectToAction(nameof(VerifyTwoFactor));
        }

        await _authService.SignInAsync(result.User!, HttpContext);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Shows the form for the 2FA code after a correct password.
    /// </summary>
    /// <remarks><c>GET /Account/VerifyTwoFactor</c>. Open to anonymous users. Needs the pending user ID from the login step in TempData.</remarks>
    /// <returns>The 2FA view, or a redirect to the login page if there is no pending login.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult VerifyTwoFactor()
    {
        if (TempData["PendingUserId"] is not int userId)
            return RedirectToAction(nameof(Login));

        TempData.Keep("PendingUserId");

        return View(new VerifyTwoFactorViewModel { UserId = userId });
    }

    /// <summary>
    /// Checks the 2FA code and signs the user in when it is valid.
    /// </summary>
    /// <remarks><c>POST /Account/VerifyTwoFactor</c>. Open to anonymous users. Requires an anti-forgery token.</remarks>
    /// <param name="model">The submitted user ID and code.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A redirect to the home page on success; otherwise the form again with errors.</returns>
    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorViewModel model, CancellationToken ct)
    {
        var result = await _authService.VerifyTwoFactorAsync(model.UserId, model.Code, ct);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        await _authService.SignInAsync(result.User!, HttpContext);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Starts 2FA setup: makes a new secret and shows it as a QR code.
    /// </summary>
    /// <remarks><c>GET /Account/SetupTwoFactor</c>. Requires a signed-in user (fallback policy). The secret is kept in TempData until the code is confirmed.</remarks>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The setup view with the QR code and the key for manual entry.</returns>
    [HttpGet]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken ct)
    {
        var model = await _authService.BeginTwoFactorSetupAsync(User.GetUserId(), ct);
        TempData["PendingSecret"] = model.ManualEntryKey;
        return View(model);
    }

    /// <summary>
    /// Confirms the first code from the authenticator app and turns on 2FA.
    /// </summary>
    /// <remarks><c>POST /Account/SetupTwoFactor</c>. Requires a signed-in user (fallback policy). Requires an anti-forgery token.</remarks>
    /// <param name="model">The submitted code.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to the home page on success, a redirect to start over if the secret has expired,
    /// or the form again with errors.
    /// </returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupTwoFactor(
        TwoFactorSetupViewModel model,
        CancellationToken ct)
    {
        if (TempData["PendingSecret"] is not string secret)
            return RedirectToAction(nameof(SetupTwoFactor));

        var result = await _authService.CompleteTwoFactorSetupAsync(
            User.GetUserId(),
            secret,
            model.Code,
            ct);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            TempData.Keep("PendingSecret");
            return View(model);
        }

        TempData["Success"] = "Two-factor authentication enabled.";

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Turns email notifications on or off for the signed-in user.
    /// </summary>
    /// <remarks><c>POST /Account/UpdateNotifications</c>. Requires a signed-in user and an anti-forgery token.</remarks>
    /// <param name="emailNotifications"><see langword="true"/> to turn notifications on; <see langword="false"/> to turn them off.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A redirect to the account page, or 404 Not Found if the user no longer exists.</returns>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNotifications(bool emailNotifications, CancellationToken ct)
    {
        var found = await _userService.SetEmailNotificationsAsync(User.GetUserId(), emailNotifications, ct);
        if (!found) return NotFound();

        TempData["Success"] = emailNotifications
            ? "Email notifications are on."
            : "Email notifications are off.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Changes the signed-in user's password and signs them in again with the new security stamp.
    /// </summary>
    /// <remarks><c>POST /Account/ChangePassword</c>. Requires a signed-in user and an anti-forgery token.</remarks>
    /// <param name="model">The submitted current password, new password and confirmation.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// The account view with a success message, the account view with errors,
    /// or 404 Not Found if the user no longer exists.
    /// </returns>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var account = await _userService.GetAccountAsync(User.GetUserId(), ct);
            return account is null ? NotFound() : View(nameof(Index), account);
        }

        var userId = User.GetUserId();
        var result = await _authService.ChangePasswordAsync(userId, model, ct);

        if (!result.Success)
        {
            var error = result.Error ?? "Failed to update password.";

            if (error.Contains("current password", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ChangePasswordViewModel.CurrentPassword), error);
            }
            else
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var account = await _userService.GetAccountAsync(userId, ct);
            return account is null ? NotFound() : View(nameof(Index), account);
        }

        await _authService.SignInAsync(result.User!, HttpContext);

        TempData["Success"] = "Password updated successfully.";
        var updatedAccount = await _userService.GetAccountAsync(userId, ct);
        return updatedAccount is null ? NotFound() : View(nameof(Index), updatedAccount);
    }

    /// <summary>
    /// Updates the signed-in user's username and email, and refreshes the sign-in cookie.
    /// </summary>
    /// <remarks><c>POST /Account/UpdateProfile</c>. Requires a signed-in user and an anti-forgery token.</remarks>
    /// <param name="model">The submitted username and email.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// The account view with a success message, the account view with errors
    /// (for example when the email or username is taken), or 404 Not Found if the user no longer exists.
    /// </returns>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(EditProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var account = await _userService.GetAccountAsync(User.GetUserId(), ct);
            return account is null ? NotFound() : View(nameof(Index), account);
        }
        var result = await _authService.UpdateProfileAsync(User.GetUserId(), model, ct);
        if (!result.Success)
        {
            var error = result.Error ?? "Failed to update profile.";

            if (error.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(AccountViewModel.Email), error);
            }
            else if (error.Contains("username", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(AccountViewModel.Username), error);
            }
            else
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var account = await _userService.GetAccountAsync(User.GetUserId(), ct);
            return account is null ? NotFound() : View(nameof(Index), account);
        }
        await _authService.SignInAsync(result.User!, HttpContext);

        TempData["Success"] = "Profile updated successfully.";
        var updatedProfileAccount = await _userService.GetAccountAsync(User.GetUserId(), ct);
        return updatedProfileAccount is null ? NotFound() : View(nameof(Index), updatedProfileAccount);
    }

    /// <summary>
    /// Signs the current user out.
    /// </summary>
    /// <remarks><c>POST /Account/Logout</c>. Requires a signed-in user (fallback policy) and an anti-forgery token.</remarks>
    /// <returns>A redirect to the home page.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Shows the page users see when they lack the role for a page.
    /// </summary>
    /// <remarks><c>GET /Account/AccessDenied</c>. Open to anonymous users.</remarks>
    /// <returns>The access denied view.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}