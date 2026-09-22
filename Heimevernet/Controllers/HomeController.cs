using System.Diagnostics;
using Heimevernet.Models;
using Heimevernet.Repositories;
using Heimevernet.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class HomeController : Controller
{
    private readonly INeedRepository _repository;

    public HomeController(INeedRepository repository)
    {
        _repository = repository;
    }

    public async Task <IActionResult> Index()
    {
        return View(await _repository.GetAllAsync());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
