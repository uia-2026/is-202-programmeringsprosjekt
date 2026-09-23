using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// Controller responsible for managing <see cref="Heimevernet.Models.Resource"/> entities.
/// </summary>
public class ResourceController : Controller
{
    private readonly IResourceService _resourceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceController"/> class.
    /// </summary>
    /// <param name="resourceService">Service used to manage resources.</param>
    public ResourceController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    /// <summary>Shows a list of available resources.</summary>
    /// <param name="cancellationToken">Cancellation token forwarded to the service call.</param>
    /// <returns>View containing the list of resources.</returns>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var resources = await _resourceService.GetAllAsync(cancellationToken);
        return View(resources);
    }

    /// <summary>Displays the create resource form with sensible defaults.</summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Resource { AvailableFrom = DateTime.Now });
    }

    /// <summary>Handles POST to create a new resource.</summary>
    /// <param name="resource">The bound resource information from the form.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to the service.</param>
    /// <returns>Redirects to Index on success or redisplays the create view when model state is invalid.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Resource resource, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(resource);
        }

        resource.Status = ResourceStatus.New;
        await _resourceService.AddAsync(resource, cancellationToken);

        TempData["Success"] = $"The resource \"{resource.Type}\" has been registered.";
        return RedirectToAction(nameof(Index));
    }
}
