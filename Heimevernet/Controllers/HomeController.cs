using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels;
using Heimevernet.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;


namespace Heimevernet.Controllers;

/// <summary>
/// Default application controller providing basic pages like index and privacy.
/// </summary>
public class HomeController : Controller
{
    private readonly INeedService _needService;
    private readonly IResourceService _resourceService;

    /// <summary>
    /// Creates a new instance of <see cref="HomeController"/>.
    /// </summary>
    /// <param name="repository">Repository used to read needs.</param>

    public HomeController(INeedService needService, IResourceService resourceService)
    {

        _needService = needService;
        _resourceService = resourceService;
    }

    /// <summary>
    /// Shows the home page listing recent needs.
    /// </summary>
    /// <returns>View with a list of needs from the repository.</returns>

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

    /// <summary>Returns the privacy information page.</summary>

    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Returns an error view with request id information.
    /// </summary>

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}