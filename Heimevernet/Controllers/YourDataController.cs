using System.Text.Json;
using System.Text.Json.Serialization;
using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// Self-service "Your data" page. Available to every logged-in user (any role),
/// and always scoped to the current user's own data.
/// </summary>
[Authorize]
public class YourDataController(IYourDataService yourDataService, IAuthService authService) : Controller
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IYourDataService _yourDataService = yourDataService;
    private readonly IAuthService _authService = authService;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = await _yourDataService.GetOverviewAsync(User.GetUserId(), ct);

        if (model == null)
            return NotFound();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var export = await _yourDataService.ExportAsync(User.GetUserId(), ct);

        if (export == null)
            return NotFound();

        var json = JsonSerializer.SerializeToUtf8Bytes(export, ExportJsonOptions);
        return File(json, "application/json", $"heimevernet-your-data-{DateTime.UtcNow:yyyy-MM-dd}.json");
    }

    /// <summary>
    /// Deletes the logged-in user's account. The user must type their username to confirm.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string? confirmUsername, CancellationToken ct)
    {
        if (!string.Equals(confirmUsername?.Trim(), User.Identity?.Name, StringComparison.Ordinal))
        {
            TempData["Error"] = "The username you typed did not match. Your account was not deleted.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _yourDataService.DeleteAccountAsync(User.GetUserId(), ct);

        if (result == DeleteAccountResult.NotFound)
            return NotFound();

        if (result == DeleteAccountResult.BlockedByActiveMatch)
        {
            TempData["Error"] = "You have an active match in progress. Resolve or cancel it before deleting your account.";
            return RedirectToAction(nameof(Index));
        }

        await _authService.SignOutAsync(HttpContext);

        TempData["Success"] = "Your account and listings have been deleted.";
        return RedirectToAction("Login", "Account");
    }
}
