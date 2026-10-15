using Heimevernet.Models;
using Heimevernet.ViewModels.Map;

namespace Heimevernet.Services.Interfaces;

public interface IMapService
{
    Task<IReadOnlyList<MapMarkerViewModel>> GetMarkersAsync(int userId, UserRole role, CancellationToken ct = default);
}