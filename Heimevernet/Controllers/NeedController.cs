using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

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

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var needs = await _needService.GetAllAsync(cancellationToken);

        return View(needs);
    }

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

        var userId = 1; // Replace with authenticated user's ID.

        await _needService.CreateAsync(model, userId, cancellationToken);

        TempData["Success"] =
            $"The need \"{model.Title}\" has been registered.";

        return RedirectToAction(nameof(Index));
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