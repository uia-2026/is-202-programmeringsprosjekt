using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// Controller that serves the application's entry points. The root/home route
/// redirects straight into the Map page, which is the actual landing
/// experience once a user is signed in.
/// </summary>
public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Map");
    }

    /// <summary>
    /// Displays the privacy page.
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }
}