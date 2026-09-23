using System.Diagnostics;
using System;
using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// Default application controller providing basic pages like index and privacy.
/// </summary>
public class HomeController : Controller
{
    private readonly INeedRepository _repository;

    /// <summary>
    /// Creates a new instance of <see cref="HomeController"/>.
    /// </summary>
    /// <param name="repository">Repository used to read needs.</param>
    public HomeController(INeedRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Shows the home page listing recent needs.
    /// </summary>
    /// <returns>View with a list of needs from the repository.</returns>
    public IActionResult Index()
    {
        return View(_repository.GetAll());
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
