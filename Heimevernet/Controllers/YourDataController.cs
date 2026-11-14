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
/// <param name="yourDataService">Builds the overview and the export.</param>
[Authorize]
public class YourDataController(IYourDataService yourDataService) : Controller
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IYourDataService _yourDataService = yourDataService;

    /// <summary>
    /// Shows the user's account details and own activity.
    /// </summary>
    /// <remarks><c>GET /YourData/Index</c>. Requires a signed-in user (any role).</remarks>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The Index view, or 404 Not Found if the user no longer exists.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = await _yourDataService.GetOverviewAsync(User.GetUserId(), ct);

        if (model == null)
            return NotFound();

        return View(model);
    }

    /// <summary>
    /// Downloads all of the user's own data as an indented JSON file.
    /// </summary>
    /// <remarks><c>GET /YourData/Export</c>. Requires a signed-in user (any role).</remarks>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>An <c>application/json</c> file download, or 404 Not Found if the user no longer exists.</returns>
    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var export = await _yourDataService.ExportAsync(User.GetUserId(), ct);

        if (export == null)
            return NotFound();

        var json = JsonSerializer.SerializeToUtf8Bytes(export, ExportJsonOptions);
        return File(json, "application/json", $"heimevernet-your-data-{DateTime.UtcNow:yyyy-MM-dd}.json");
    }

}