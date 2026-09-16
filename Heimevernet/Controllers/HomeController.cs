using System.Diagnostics;
using Heimevernet.Models;
using Heimevernet.Services;
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

    public IActionResult Index()
    {
        return View(_repository.GetAll());
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
