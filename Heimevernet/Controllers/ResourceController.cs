using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// Controller responsible for managing <see cref="Heimevernet.Models.Resource"/> entities.
/// </summary>
public class ResourceController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ICategoryRepository _categoryRepository;


    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceController"/> class.
    /// </summary>
    /// <param name="resourceService">Service used to manage resources.</param>

    public ResourceController(
        IResourceService resourceService,
        ICategoryRepository categoryRepository)
    {
        _resourceService = resourceService;
        _categoryRepository = categoryRepository;
    }

    /// <summary>Shows a list of available resources.</summary>
    /// <param name="cancellationToken">Cancellation token forwarded to the service call.</param>
    /// <returns>View containing the list of resources.</returns>

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var resources =
            await _resourceService.GetAllAsync(cancellationToken);

        return View(resources);
    }

    /// <summary>Displays the create resource form with sensible defaults.</summary>
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

    /// <summary>Handles POST to create a new resource.</summary>
    /// <param name="resource">The bound resource information from the form.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to the service.</param>
    /// <returns>Redirects to Index on success or redisplays the create view when model state is invalid.</returns>
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