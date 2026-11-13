using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for account pages: registration, login, two-factor authentication (2FA),
/// account settings and logout.
/// </summary>
/// <remarks>
/// The app has a fallback policy that requires a signed-in user, so actions without
/// <see cref="AllowAnonymousAttribute"/> are only for signed-in users.
/// </remarks>
/// <param name="authService">Handles registration, login, 2FA and the auth cookie.</param>
/// <param name="userService">Reads and updates account settings.</param>
public class AccountController(IAuthService authService, IUserService userService) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Shows the account page for the signed-in user.
    /// </summary>
    /// <remarks><c>GET /Account/Index</c>. Requires a signed-in user (any role).</remarks>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The Index view with an <see cref="AccountViewModel"/>, or 404 Not Found if the user no longer exists.</returns>
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
    /// <remarks><c>GET /Account/Register</c>. Anonymous access allowed.</remarks>
    /// <returns>The Register view with an empty <see cref="RegisterViewModel"/>.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    /// <summary>
    /// Creates a new account from the submitted form.
    /// </summary>
    /// <remarks><c>POST /Account/Register</c>. Anonymous access allowed. Requires an anti-forgery token.</remarks>
    /// <param name="model">The submitted registration form.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A redirect to <see cref="Login()"/> on success, or the Register view with errors (for example a duplicate email).</returns>
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
    /// <remarks><c>GET /Account/Login</c>. Anonymous access allowed. Unauthenticated users are sent here by the cookie handler.</remarks>
    /// <returns>The Login view with an empty <see cref="LoginViewModel"/>.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    /// <summary>
    /// Checks the email and password. If the user has 2FA on, the user ID is kept in TempData and the
    /// user is sent to the 2FA step; otherwise the user is signed in.
    /// </summary>
    /// <remarks><c>POST /Account/Login</c>. Anonymous access allowed. Requires an anti-forgery token.</remarks>
    /// <param name="model">The submitted login form.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to <see cref="VerifyTwoFactor()"/> when 2FA is needed, a redirect to Home/Index after sign-in,
    /// or the Login view with an error.
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
    /// Shows the form for the 2FA code during login.
    /// </summary>
    /// <remarks><c>GET /Account/VerifyTwoFactor</c>. Anonymous access allowed, but only works right after a correct password.</remarks>
    /// <returns>The VerifyTwoFactor view, or a redirect to <see cref="Login()"/> when there is no pending login.</returns>
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
    /// Checks the 2FA code and signs the user in if it is valid.
    /// </summary>
    /// <remarks><c>POST /Account/VerifyTwoFactor</c>. Anonymous access allowed. Requires an anti-forgery token.</remarks>
    /// <param name="model">The submitted user ID and code.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A redirect to Home/Index on success, or the VerifyTwoFactor view with an error.</returns>
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
    /// Starts 2FA setup: creates a new secret, keeps it in TempData, and shows it with a QR code.
    /// </summary>
    /// <remarks><c>GET /Account/SetupTwoFactor</c>. Requires a signed-in user (fallback policy).</remarks>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The SetupTwoFactor view with a <see cref="TwoFactorSetupViewModel"/>.</returns>
    [HttpGet]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken ct)
    {
        var model = await _authService.BeginTwoFactorSetupAsync(User.GetUserId(), ct);
        TempData["PendingSecret"] = model.ManualEntryKey;
        return View(model);
    }

    /// <summary>
    /// Finishes 2FA setup by checking a code against the pending secret and turning 2FA on.
    /// </summary>
    /// <remarks><c>POST /Account/SetupTwoFactor</c>. Requires a signed-in user (fallback policy) and an anti-forgery token.</remarks>
    /// <param name="model">The submitted code.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>
    /// A redirect to Home/Index on success, the SetupTwoFactor view with an error for a wrong code,
    /// or a redirect to the GET action when the pending secret has expired.
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
    /// <remarks><c>POST /Account/UpdateNotifications</c>. Requires a signed-in user (any role) and an anti-forgery token.</remarks>
    /// <param name="emailNotifications"><see langword="true"/> to turn notifications on.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>A redirect to <see cref="Index"/>, or 404 Not Found if the user no longer exists.</returns>
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
    /// Signs the current user out.
    /// </summary>
    /// <remarks><c>POST /Account/Logout</c>. Requires a signed-in user (fallback policy) and an anti-forgery token.</remarks>
    /// <returns>A redirect to Home/Index (which then sends the user to the login page).</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Shows the "access denied" page for signed-in users who lack the needed role.
    /// </summary>
    /// <remarks><c>GET /Account/AccessDenied</c>. Anonymous access allowed.</remarks>
    /// <returns>The AccessDenied view.</returns>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}