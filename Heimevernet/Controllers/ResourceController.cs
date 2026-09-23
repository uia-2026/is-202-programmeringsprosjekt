using Heimevernet.Mappers;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class ResourceController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ResourceMapper _resourceMapper;

    public ResourceController(
        IResourceService resourceService,
        ICategoryRepository categoryRepository,
        ResourceMapper resourceMapper)
    {
        _resourceService = resourceService;
        _categoryRepository = categoryRepository;
        _resourceMapper = resourceMapper;
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

        var resource =
            _resourceMapper.ToEntity(model, userId);

        resource.Status = ResourceStatus.Available;

        await _resourceService.AddAsync(
            model,
            userId,
            cancellationToken);

        TempData["Success"] =
            $"The resource \"{resource.Title}\" has been registered.";

        return RedirectToAction(nameof(Index));
    }
}