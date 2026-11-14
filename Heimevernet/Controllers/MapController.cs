using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Map;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// MVC controller for the map page and the JSON marker data it loads.
/// </summary>
/// <remarks>Requires a signed-in user (fallback policy).</remarks>
/// <param name="mapService">Builds the marker data.</param>
public class MapController(IMapService mapService) : Controller
{
    private readonly IMapService _mapService = mapService;

    /// <summary>
    /// Shows the map page. Markers are loaded afterwards from <see cref="Data"/>.
    /// </summary>
    /// <remarks><c>GET /Map/Index</c>. Requires a signed-in user.</remarks>
    /// <returns>The Index view.</returns>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// Returns need and resource markers inside the visible map area as JSON.
    /// </summary>
    /// <remarks><c>GET /Map/Data?south=..&amp;north=..&amp;west=..&amp;east=..</c>. Requires a signed-in user.</remarks>
    /// <param name="bounds">The visible map area from the query string.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>200 OK with a <see cref="Heimevernet.Services.MapDataViewModel"/> as JSON, or 400 Bad Request when the bounds are invalid.</returns>
    [HttpGet]
    public async Task<IActionResult> Data([FromQuery] MapBoundsRequest bounds, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var data = await _mapService.GetMarkersAsync(
            User.GetUserId(), User.GetRole(), bounds.ToGeoBox(), cancellationToken);

        return Json(data);
    }
}