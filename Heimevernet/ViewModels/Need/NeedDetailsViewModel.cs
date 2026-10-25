namespace Heimevernet.ViewModels.Need;

public class NeedDetailsViewModel
{
    public required NeedViewModel Need { get; init; }

    public IReadOnlyList<NearbyResourceViewModel> NearbyResources
    { get; init; } = [];

    public double NearbyResourcesRadiusKm { get; init; }

    public bool CanProposeMatch { get; init; }
}