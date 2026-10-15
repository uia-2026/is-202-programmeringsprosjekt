using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Map;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class MapController : Controller
{
    private readonly IMapService _mapService;

    public MapController(IMapService mapService) => _mapService = mapService;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Data([FromQuery] MapBoundsRequest bounds, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var data = await _mapService.GetMarkersAsync(
            User.GetUserId(), User.GetRole(), bounds.ToGeoBox(), cancellationToken);

        return Json(data);
    }
}