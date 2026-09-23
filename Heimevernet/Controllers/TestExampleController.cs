using Heimevernet.Mapping;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.TestExample;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// Example controller used to demonstrate mapping and display of resources in the UI.
/// </summary>
public class TestExampleController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<TestExampleController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TestExampleController"/>.
    /// </summary>
    /// <param name="resourceService">Service providing resources.</param>
    /// <param name="logger">Logger instance for the controller.</param>
    public TestExampleController(IResourceService resourceService, ILogger<TestExampleController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    /// <summary>Displays example data for testing and demonstration purposes.</summary>
    /// <param name="cancellationToken">Cancellation token forwarded to the service call.</param>
    /// <returns>View model populated with resource view models.</returns>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var entities = await _resourceService.GetAllAsync(cancellationToken);

        var viewModel = new TestExampleIndexViewModel
        {
            Resources = entities.ToViewModel()
        };

        if (viewModel.IsEmpty)
        {
            _logger.LogInformation("No resources found in database");
        }

        return View(viewModel);
    }
}
