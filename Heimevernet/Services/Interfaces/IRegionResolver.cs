using System.Threading;
using System.Threading.Tasks;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Finds the Norwegian region (county) for a coordinate by reverse-geocoding.
/// </summary>
public interface IRegionResolver
{
    /// <summary>
    /// Try to resolve a human-readable region name for the provided coordinates.
    /// Returns a result indicating whether the lookup completed and whether the location is within Norway.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees.</param>
    /// <param name="longitude">Longitude in decimal degrees.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="RegionResolutionResult"/>. Never throws for network errors; those give <see cref="RegionResolutionStatus.Unavailable"/>.</returns>
    Task<RegionResolutionResult> TryResolveRegionAsync(double latitude, double longitude, CancellationToken cancellationToken);
}

/// <summary>
/// How a region lookup ended.
/// </summary>
public enum RegionResolutionStatus
{
    /// <summary>The lookup worked and returned an address.</summary>
    Resolved,
    /// <summary>The lookup worked but there is no address at this point (for example at sea).</summary>
    NoAddress,
    /// <summary>The lookup service could not be reached or returned an error.</summary>
    Unavailable
}

/// <summary>
/// Result of <see cref="IRegionResolver.TryResolveRegionAsync"/>.
/// </summary>
public sealed class RegionResolutionResult
{
    /// <summary>
    /// <see langword="true"/> when the lookup returned an address.
    /// </summary>
    public bool Success { get; init; }
    /// <summary>
    /// <see langword="true"/> when the address is in Norway.
    /// </summary>
    public bool IsInNorway { get; init; }
    /// <summary>
    /// The region (county) name, or <see langword="null"/> when it could not be found.
    /// </summary>
    public string? Region { get; init; }
    /// <summary>
    /// How the lookup ended. Defaults to <see cref="RegionResolutionStatus.Unavailable"/>.
    /// </summary>
    public RegionResolutionStatus Status { get; init; } = RegionResolutionStatus.Unavailable;
}