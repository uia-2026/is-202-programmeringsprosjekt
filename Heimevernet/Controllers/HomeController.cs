<<<<<<< HEAD
using System.Diagnostics;
using System;
using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.ViewModels;
||||||| 0dd174d
using System.Diagnostics;
using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.ViewModels;
=======
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Home;
>>>>>>> main
using Microsoft.AspNetCore.Mvc;
using System.Threading;


namespace Heimevernet.Controllers;

/// <summary>
/// Default application controller providing basic pages like index and privacy.
/// </summary>
public class HomeController : Controller
{
    private readonly INeedService _needService;
    private readonly IResourceService _resourceService;

<<<<<<< HEAD
    /// <summary>
    /// Creates a new instance of <see cref="HomeController"/>.
    /// </summary>
    /// <param name="repository">Repository used to read needs.</param>
    public HomeController(INeedRepository repository)
||||||| 0dd174d
    public HomeController(INeedRepository repository)
=======
    public HomeController(INeedService needService, IResourceService resourceService)
>>>>>>> main
    {
<<<<<<< HEAD
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
||||||| 0dd174d
        _repository = repository;
=======
        _needService = needService;
        _resourceService = resourceService;
>>>>>>> main
    }

<<<<<<< HEAD
    /// <summary>
    /// Shows the home page listing recent needs.
    /// </summary>
    /// <returns>View with a list of needs from the repository.</returns>
    public IActionResult Index()
    {
        return View(_repository.GetAll());
||||||| 0dd174d
    public IActionResult Index()
    {
        return View(_repository.GetAll());
=======
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
>>>>>>> main
    }

<<<<<<< HEAD
    /// <summary>Returns the privacy information page.</summary>
||||||| 0dd174d
=======

>>>>>>> main
    public IActionResult Privacy()
    {
        return View();
    }
<<<<<<< HEAD

    /// <summary>
    /// Returns an error view with request id information.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
||||||| 0dd174d

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
=======
}
>>>>>>> main
