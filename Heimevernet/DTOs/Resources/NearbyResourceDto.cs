namespace Heimevernet.DTOs.Resources;

/// <summary>
/// A resource near a need, read from the database with its distance.
/// Mapped to <see cref="Heimevernet.ViewModels.Need.NearbyResourceViewModel"/>.
/// </summary>
/// <param name="Id">ID of the resource.</param>
/// <param name="Title">Title of the resource.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="Region">Region name.</param>
/// <param name="Status">Status name.</param>
/// <param name="ContactPoint">Contact details.</param>
/// <param name="QuantityOffered">Quantity the resource offers.</param>
/// <param name="DistanceKm">Distance from the need, in kilometres.</param>
public record NearbyResourceDto(
    int Id,
    string Title,
    string CategoryName,
    string Region,
    string Status,
    string ContactPoint,
    int QuantityOffered,
    double DistanceKm
);