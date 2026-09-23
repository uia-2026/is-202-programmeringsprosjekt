using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Home;
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
        var needs = await _needService.GetSummariesAsync();

        var viewModel = new HomeIndexViewModel
        {
            Needs = needs
        };

        return View(viewModel);
    }


    public IActionResult Privacy()
    {
        return View();
    }
}