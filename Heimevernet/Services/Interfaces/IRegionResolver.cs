using System.Threading;
using System.Threading.Tasks;

namespace Heimevernet.Services.Interfaces;

public interface IRegionResolver
{
    /// <summary>
    /// Try to resolve a human-readable region name for the provided coordinates.
    /// Returns a result indicating whether the lookup completed and whether the location is within Norway.
    /// </summary>
    Task<RegionResolutionResult> TryResolveRegionAsync(double latitude, double longitude, CancellationToken cancellationToken);
}

public sealed class RegionResolutionResult
{
    public bool Success { get; init; }
    public bool IsInNorway { get; init; }
    public string? Region { get; init; }
}
