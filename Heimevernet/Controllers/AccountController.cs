using Heimevernet.Extensions;
using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class AccountController(IAuthService authService, IUserService userService) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly IUserService _userService = userService;

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await _userService.GetAccountAsync(User.GetUserId(), ct);

        if (user == null)
            return NotFound();

        return View(user);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

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

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

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

    [AllowAnonymous]
    [HttpGet]
    public IActionResult VerifyTwoFactor()
    {
        if (TempData["PendingUserId"] is not int userId)
            return RedirectToAction(nameof(Login));

        TempData.Keep("PendingUserId");

        return View(new VerifyTwoFactorViewModel { UserId = userId });
    }

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

    [HttpGet]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken ct)
    {
        var model = await _authService.BeginTwoFactorSetupAsync(User.GetUserId(), ct);
        TempData["PendingSecret"] = model.ManualEntryKey;
        return View(model);
    }

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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([Bind(Prefix = "ChangePassword")] ChangePasswordViewModel model, CancellationToken ct)
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
            ModelState.AddModelError(string.Empty, result.Error ?? "Unable to change password.");
            var account = await _userService.GetAccountAsync(userId, ct);
            return account is null ? NotFound() : View(nameof(Index), account);
        }

        await _authService.SignInAsync(result.User!, HttpContext);

        TempData["Success"] = "Success";
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([Bind(Prefix = "EditProfile")]EditProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var account = await _userService.GetAccountAsync(User.GetUserId(), ct);
            return account is null ? NotFound() : View(nameof(Index), account);
        }
        var result = await _authService.UpdateProfileAsync(User.GetUserId(), model, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, "Failed to update profile.");
            var account = await _userService.GetAccountAsync(User.GetUserId(), ct);
            return account is null ? NotFound() : View(nameof(Index), account);
        }
        await _authService.SignInAsync(result.User!, HttpContext);

        TempData["Success"] = "Success";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}