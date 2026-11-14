namespace Heimevernet.ViewModels.Need;

/// <summary>
/// Data for the need details page: the need, nearby resources and whether the user can propose a match.
/// </summary>
public class NeedDetailsViewModel
{
    /// <summary>The need being shown.</summary>
    public required NeedViewModel Need { get; init; }

    /// <summary>Available resources of the same category near the need, nearest first.</summary>
    public IReadOnlyList<NearbyResourceViewModel> NearbyResources
    { get; init; } = [];

    /// <summary>The search radius used for <see cref="NearbyResources"/>, shown in the heading.</summary>
    public double NearbyResourcesRadiusKm { get; init; }

    /// <summary><see langword="true"/> when the current user owns the need and it is open, so the "Propose" button is shown.</summary>
    public bool CanProposeMatch { get; init; }
}