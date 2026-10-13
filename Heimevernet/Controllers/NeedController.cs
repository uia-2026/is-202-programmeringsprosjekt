using Heimevernet.Extensions;
using System.Net.Http;
using System.Text.Json;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for handling Need-related web pages (list, create, details).
/// Uses INeedService for business operations and ICategoryRepository to populate category lists.
/// </summary>
public class NeedController : Controller
{
    private readonly INeedService _needService;
    private readonly ICategoryRepository _categoryRepository;

    public NeedController(
        INeedService needService,
        ICategoryRepository categoryRepository)
    {
        _needService = needService;
        _categoryRepository = categoryRepository;
    }

    /// <summary>
    /// Displays the list of needs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>An <see cref="IActionResult"/> that renders the Index view.</returns>
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var needs = await _needService.GetAllAsync(cancellationToken);

        return View(needs);
    }

    /// <summary>
    /// Shows the create need form populated with available categories.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> that renders the Create view.</returns>
    [HttpGet]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Create()
    {
        var model = new NeedCreateViewModel
        {
            AvailableCategories =
                await _categoryRepository.GetAllAsync()
        };

        return View(model);
    }

    /// <summary>
    /// Handles posted create need form. Validates and persists the new need.
    /// </summary>
    /// <param name="model">The create view model submitted by the user.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Redirects to Index on success or returns the view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "PublicActor")]
    public async Task<IActionResult> Create(
        NeedCreateViewModel model,
        CancellationToken cancellationToken)
    {
        // Ensure deadline (if provided) is in the future
        if (model.Deadline.HasValue)
        {
            var deadlineUtc = model.Deadline.Value.ToUniversalTime();
            if (deadlineUtc <= DateTime.UtcNow)
            {
                ModelState.AddModelError(nameof(model.Deadline), "Deadline must be in the future.");
            }
        }

        // If coordinates are provided, derive the county (region) server-side to ensure integrity.
        // If the coordinates are outside Norway/Svalbard, set Region to "International".
        if (model.Latitude != 0 || model.Longitude != 0)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={model.Latitude}&lon={model.Longitude}&accept-language=nb";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Heimevernet/1.0 (contact@heimevernet.example)");

                using var resp = await client.GetAsync(url, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                    if (doc.RootElement.TryGetProperty("address", out var addr))
                    {
                        var countryCode = addr.TryGetProperty("country_code", out var ccEl) ? ccEl.GetString()?.ToLowerInvariant() : null;
                        var isNorway = countryCode == "no" || countryCode == "sj";

                        // Prefer county, fall back to state if present
                        var county = addr.TryGetProperty("county", out var countyEl) ? countyEl.GetString() : null;
                        if (string.IsNullOrEmpty(county) && addr.TryGetProperty("state", out var stateEl))
                        {
                            county = stateEl.GetString();
                        }

                        if (isNorway)
                        {
                            model.Region = !string.IsNullOrEmpty(county) ? county : "Unknown county";
                        }
                        else
                        {
                            model.Region = "International";
                        }
                    }

        // If we resolved region server-side, remove any ModelState entry for Region so
        // validation reflects the server-set value rather than a missing client input.
        if (model.Latitude != 0 || model.Longitude != 0)
        {
            ModelState.Remove(nameof(model.Region));
        }
                    else
                    {
                        // No address object; fall back to International
                        model.Region = "International";
                    }
                }
                else
                {
                    // Could not verify; fall back to International
                    model.Region = "International";
                }
            }
            catch
            {
                // On error, do not block creation; treat as International
                model.Region = "International";
            }
        }

        if (!ModelState.IsValid)
        {
            model.AvailableCategories =
                await _categoryRepository.GetAllAsync();

            return View(model);
        }

        var createdId = await _needService.CreateAsync(model, User.GetUserId(), cancellationToken);

        TempData["Success"] =
            $"The need \"{model.Title}\" has been registered.";

        return RedirectToAction(nameof(Details), new { id = createdId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var need = await _needService.GetByIdAsync(id, cancellationToken);

        if (need == null)
        {
            return NotFound();
        }

        return View(need);
    }
}