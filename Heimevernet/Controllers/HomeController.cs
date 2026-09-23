using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class HomeController : Controller
{
    private readonly INeedService _needService;

    public HomeController(INeedService needService)
    {
        _needService = needService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var needs = await _needService.GetAllAsync();

        return View(needs);
    }
}