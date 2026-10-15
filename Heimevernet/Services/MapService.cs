using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Map;

public sealed class MapService : IMapService
{
    private readonly INeedService _needService;
    private readonly IResourceService _resourceService;

    public MapService(INeedService needService, IResourceService resourceService)
    {
        _needService = needService;
        _resourceService = resourceService;
    }

    public async Task<IReadOnlyList<MapMarkerViewModel>> GetMarkersAsync(
        int userId,
        UserRole role,
        CancellationToken ct = default)
    {
        var needs = await _needService.GetMapMarkersAsync(ct);

        var resources = role switch
        {
            UserRole.ResourceProvider =>
                await _resourceService.GetOwnMapMarkersAsync(userId, ct),

            UserRole.PublicActor =>
                await _resourceService.GetAvailableMapMarkersAsync(ct),

            _ => throw new UnauthorizedAccessException()
        };

        return needs.Concat(resources).ToList();
    }
}