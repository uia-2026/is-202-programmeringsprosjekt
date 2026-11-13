using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// Controller that serves the application's entry points. The root/home route
/// redirects straight into the Map page, which is the actual landing
/// experience once a user is signed in.
/// </summary>
public class HomeController : Controller
{
    /// <summary>
    /// Entry point of the site. Sends the user straight on to the map page.
    /// </summary>
    /// <remarks><c>GET /</c> or <c>GET /Home/Index</c>. Requires a signed-in user (fallback policy).</remarks>
    /// <returns>A redirect to Map/Index.</returns>
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Map");
    }

    /// <summary>
    /// Displays the public privacy policy. Reachable without logging in so
    /// visitors can read it before creating an account.
    /// </summary>
    /// <remarks><c>GET /Privacy</c>. Anonymous access allowed.</remarks>
    /// <returns>The Privacy view.</returns>
    [AllowAnonymous]
    [HttpGet("/Privacy")]
    public IActionResult Privacy()
    {
        return View();
    }
}