using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using System.Threading;


namespace Heimevernet.Controllers;

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
        var needs = await _needService.GetSummariesAsync();
        var resources = await _resourceService.GetAllAsync(cancellationToken);

        var viewModel = new HomeIndexViewModel
        {
            Needs = needs,
            Resources = resources
        };

        return View(viewModel);
    }


    public IActionResult Privacy()
    {
        return View();
    }
}