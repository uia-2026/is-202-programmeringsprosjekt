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
public class YourDataController(IYourDataService yourDataService) : Controller
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IYourDataService _yourDataService = yourDataService;

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

}
