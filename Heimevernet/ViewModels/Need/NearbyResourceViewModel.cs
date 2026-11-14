namespace Heimevernet.ViewModels.Need;


/// <summary>
/// A resource near a need, shown in the "nearby resources" list on the need details page.
/// </summary>
/// <param name="Id">ID of the resource.</param>
/// <param name="Title">Title of the resource.</param>
/// <param name="CategoryName">Category name.</param>
/// <param name="Region">Region name.</param>
/// <param name="Status">Status name.</param>
/// <param name="ContactPoint">Contact details.</param>
/// <param name="QuantityOffered">Quantity the resource offers.</param>
/// <param name="DistanceKm">Distance from the need, in kilometres.</param>
public record NearbyResourceViewModel(
    int Id,
    string Title,
    string CategoryName,
    string Region,
    string Status,
    string ContactPoint,
    int QuantityOffered,
    double DistanceKm
);