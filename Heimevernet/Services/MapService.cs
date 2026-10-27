using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Map;

namespace Heimevernet.Services;

public sealed record MapDataViewModel(IReadOnlyList<MapMarkerViewModel> Markers, bool Truncated);

public sealed class MapService(INeedService needService, IResourceService resourceService) : IMapService
{
    private const int MaxPerKind = 500;

    private readonly INeedService _needService = needService;
    private readonly IResourceService _resourceService = resourceService;

    public async Task<MapDataViewModel> GetMarkersAsync(
        int userId, UserRole role, GeoBox box, CancellationToken ct = default)
    {
        // Ask for one extra row per kind: if it comes back, there was more than we show
        var needs = await _needService.GetMapMarkersAsync(box, MaxPerKind + 1, ct);

        var resources = role switch
        {
            UserRole.ResourceProvider =>
                await _resourceService.GetMapMarkersAsync(box, userId, availableOnly: false, MaxPerKind + 1, ct),
            UserRole.PublicActor =>
                await _resourceService.GetMapMarkersAsync(box, ownerUserId: null, availableOnly: true, MaxPerKind + 1, ct),
            _ => throw new UnauthorizedAccessException()
        };

        var truncated = needs.Count > MaxPerKind || resources.Count > MaxPerKind;
        var markers = needs.Take(MaxPerKind).Concat(resources.Take(MaxPerKind)).ToList();

        return new MapDataViewModel(markers, truncated);
    }
}