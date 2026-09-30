using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
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
        var secret = TempData["PendingSecret"] as string;

        if (secret == null)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }
}