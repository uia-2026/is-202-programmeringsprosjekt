using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

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