using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.TestExample;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class TestExampleController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<TestExampleController> _logger;

    public TestExampleController(
        IResourceService resourceService,
        ILogger<TestExampleController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var resources =
            await _resourceService.GetAllAsync(cancellationToken);

        var viewModel = new TestExampleIndexViewModel
        {
            Resources = resources
        };

        if (viewModel.IsEmpty)
        {
            _logger.LogInformation("No resources found in database");
        }

        return View(viewModel);
    }
}