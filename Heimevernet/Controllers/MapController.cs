using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Map;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

public class MapController : Controller
{
    private readonly INeedService _needService;
    private readonly IResourceService _resourceService;

    public MapController(INeedService needService, IResourceService resourceService)
    {
        _needService = needService;
        _resourceService = resourceService;
    }

    public IActionResult Index()
    {
        return View();
    }

    // combined Need + Resource markers for the Leaflet layer.
    [HttpGet]
    public async Task<IActionResult> Data(CancellationToken cancellationToken)
    {
        var needs = await _needService.GetAllAsync(cancellationToken);
        var resources = await _resourceService.GetAllAsync(cancellationToken);

        var needMarkers = needs.Select(n => new MapMarkerViewModel(
            Id: n.Id,
            Kind: "need",
            Title: n.Title,
            Category: n.CategoryName,
            Description: n.Description,
            Region: n.Region,
            Priority: n.Priority.ToString(),
            Status: n.Status.ToString(),
            MarkerColor: n.Priority == NeedPriority.Urgent ? "red" : "yellow",
            When: n.Deadline,
            ContactInfo: n.ContactPoint,
            Latitude: n.Latitude,
            Longitude: n.Longitude
        ));

        var resourceMarkers = resources.Select(r => new MapMarkerViewModel(
            Id: r.Id,
            Kind: "resource",
            Title: r.Title,
            Category: r.CategoryName,
            Description: r.Description,
            Region: r.Region,
            Priority: null,
            Status: r.Status.ToString(),
            MarkerColor: "green",
            When: r.AvailableFrom,
            ContactInfo: r.ContactPoint,
            Latitude: r.Latitude,
            Longitude: r.Longitude
        ));

        return Json(needMarkers.Concat(resourceMarkers));
    }
}