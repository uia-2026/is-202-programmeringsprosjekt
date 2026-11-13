using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;
using Heimevernet.Services;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Builds the marker data for the map page.
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Returns needs and resources inside the visible map area. Resource providers see all of their
    /// own resources; public actors see every available resource. At most 500 markers of each kind are returned.
    /// </summary>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="role">Role of the signed-in user. Decides which resources are shown.</param>
    /// <param name="box">The visible map area.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A <see cref="MapDataViewModel"/> with the markers, and <c>Truncated</c> set when some were left out.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when <paramref name="role"/> is not a role that can use the map.</exception>
    Task<MapDataViewModel> GetMarkersAsync(int userId, UserRole role, GeoBox box, CancellationToken ct = default);
}