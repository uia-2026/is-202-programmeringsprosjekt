using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class ResourceController : Controller
{
    private readonly IResourceService _resourceService;

    public ResourceController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var resources = await _resourceService.GetAllAsync(cancellationToken);
        return View(resources);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Resource { AvailableFrom = DateTime.Now });
    }

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
