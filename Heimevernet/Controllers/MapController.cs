using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class MapController : Controller
{
    private readonly IMapService _mapService;

    public MapController(IMapService mapService) => _mapService = mapService;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Data(CancellationToken cancellationToken)
    {
        var markers = await _mapService.GetMarkersAsync(User.GetUserId(), User.GetRole(), cancellationToken);
        return Json(markers);
    }
}