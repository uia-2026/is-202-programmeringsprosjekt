using Heimevernet.Extensions;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Heimevernet.Models;
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
    private readonly IResourceService _resourceService;

    private const double NearbyResourcesRadiusKm = 50;
    private const int NearbyResourcesMaxResults = 5;

    public NeedController(
        INeedService needService,
        ICategoryRepository categoryRepository,
        IResourceService resourceService)
    {
        _needService = needService;
        _categoryRepository = categoryRepository;
        _resourceService = resourceService;
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
    [Authorize(Roles = nameof(UserRole.PublicActor))]
    [HttpGet]
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
    [Authorize(Roles = nameof(UserRole.PublicActor))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        NeedCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories =
                await _categoryRepository.GetAllAsync();

            return View(model);
        }

        await _needService.CreateAsync(model, User.GetUserId(), cancellationToken);

        TempData["Success"] =
            $"The need \"{model.Title}\" has been registered.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Shows a single need's details, plus a distance-sorted list of the nearest
    /// available resources (within <see cref="NearbyResourcesRadiusKm"/> km), capped at
    /// <see cref="NearbyResourcesMaxResults"/> results. This is a plain "what's physically
    /// closest" list — no category/priority scoring, that belongs to a future matching feature.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var need = await _needService.GetByIdAsync(id, cancellationToken);

        if (need == null)
        {
            return NotFound();
        }

        var resources = await _resourceService.GetAllAsync(cancellationToken);

        var nearbyResources = resources
            .Select(r => new NearbyResourceViewModel(
                Id: r.Id,
                Title: r.Title,
                CategoryName: r.CategoryName,
                Region: r.Region,
                Status: r.Status.ToString(),
                ContactPoint: r.ContactPoint,
                DistanceKm: DistanceKm(need.Latitude, need.Longitude, r.Latitude, r.Longitude)))
            .Where(r => r.DistanceKm <= NearbyResourcesRadiusKm)
            .OrderBy(r => r.DistanceKm)
            .Take(NearbyResourcesMaxResults)
            .ToList();

        ViewBag.NearbyResources = nearbyResources;
        ViewBag.NearbyResourcesRadiusKm = NearbyResourcesRadiusKm;

        return View(need);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }
}