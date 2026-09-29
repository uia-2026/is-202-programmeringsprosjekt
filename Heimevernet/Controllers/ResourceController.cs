using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for handling Resource-related web pages (list, create, details).
/// Uses IResourceService for business operations and ICategoryRepository to populate category lists.
/// </summary>
public class ResourceController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ICategoryRepository _categoryRepository;

    public ResourceController(
        IResourceService resourceService,
        ICategoryRepository categoryRepository)
    {
        _resourceService = resourceService;
        _categoryRepository = categoryRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var resources =
            await _resourceService.GetAllAsync(cancellationToken);

        return View(resources);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var model = new ResourceCreateViewModel
        {
            AvailableFrom = DateTime.Now,
            AvailableCategories =
                await _categoryRepository.GetAllAsync()
        };

        return View(model);
    }

    /// <summary>
    /// Handles posted create resource form. Validates and persists the new resource.
    /// </summary>
    /// <param name="model">The create view model submitted by the user.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Redirects to Index on success or returns the view with validation errors.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ResourceCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableCategories =
                await _categoryRepository.GetAllAsync();

            return View(model);
        }

        var userId = 1; // Replace with authenticated user's ID.

        await _resourceService.CreateAsync(
            model,
            userId,
            cancellationToken);

        TempData["Success"] =
            $"The resource \"{model.Title}\" has been registered.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(
            id,
            cancellationToken);

        if (resource == null)
        {
            return NotFound();
        }

        return View(resource);
    }
}