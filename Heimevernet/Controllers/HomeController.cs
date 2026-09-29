using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;


namespace Heimevernet.Controllers;

/// <summary>
/// Controller that serves the application home pages (index and privacy).
/// Index aggregates summaries of needs and resources for the landing page.
/// </summary>
public class HomeController : Controller
{
    private readonly INeedService _needService;
    private readonly IResourceService _resourceService;

    public HomeController(INeedService needService, IResourceService resourceService)
    {
        _needService = needService;
        _resourceService = resourceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var needs = await _needService.GetSummariesAsync(cancellationToken);
        var resources = await _resourceService.GetAllAsync(cancellationToken);

        var viewModel = new HomeIndexViewModel
        {
            Needs = needs,
            Resources = resources
        };

        return View(viewModel);
    }


    /// <summary>
    /// Displays the privacy page.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> that renders the Privacy view.</returns>
    public IActionResult Privacy()
    {
        return View();
    }
}
