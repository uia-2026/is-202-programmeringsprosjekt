using Heimevernet.Extensions;
using System.Text.Json;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Heimevernet.Services.Interfaces;
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
    private readonly IRegionResolver _regionResolver;

    public NeedController(
        INeedService needService,
        ICategoryRepository categoryRepository,
        IRegionResolver regionResolver)
    {
        _needService = needService;
        _categoryRepository = categoryRepository;
        _regionResolver = regionResolver;
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
        // Deadline validation is handled by the view model attribute (FutureDate).

        // If coordinates are provided, derive the county (region) server-side to ensure integrity.
        if (model.Latitude != 0 || model.Longitude != 0)
        {
            var res = await _regionResolver.TryResolveRegionAsync(model.Latitude, model.Longitude, cancellationToken);

            if (res.Success)
            {
                if (res.IsInNorway)
                {
                    model.Region = !string.IsNullOrEmpty(res.Region) ? res.Region! : "Unknown county";
                }
                else
                {
                    // Explicitly resolved to an international location
                    model.Region = "International";
                }

                // Server derived region; remove client-side Region validation state.
                ModelState.Remove(nameof(model.Region));
            }
            else
            {
                // Lookup failed (timeout/network/HTTP error) or resolved to no region (e.g., ocean).
                ModelState.AddModelError(nameof(model.Region), "Please select a location on land (not in the ocean).");
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