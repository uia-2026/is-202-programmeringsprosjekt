using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

public interface IMapService
{
    Task<MapDataViewModel> GetMarkersAsync(int userId, UserRole role, GeoBox box, CancellationToken ct = default);
}