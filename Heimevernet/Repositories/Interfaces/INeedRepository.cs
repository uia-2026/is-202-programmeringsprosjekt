using Heimevernet.DTOs.Map;
using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access contract for Need entities.
/// Implementations provide methods to query and persist Need instances.
/// </summary>
public interface INeedRepository
{
    Task<IEnumerable<Need>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Need?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Need?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Need need, CancellationToken cancellationToken = default);
    void Remove(Need need);
    Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(GeoBox box, int limit, CancellationToken cancellationToken = default);
    void SetOriginalVersion(Need need, int version);
}

