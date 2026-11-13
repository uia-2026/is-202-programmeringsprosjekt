namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model for the print-friendly, region-filtered Resource list (#41).
/// </summary>
public class PrintByRegionViewModel
{
    /// <summary>The chosen region, or <see langword="null"/> for all regions.</summary>
    public string? SelectedRegion { get; set; }

    /// <summary>Regions that have at least one available resource, for the region picker.</summary>
    public IEnumerable<string> AvailableRegions { get; set; } = Enumerable.Empty<string>();

    /// <summary>Available resources in the chosen region, sorted by title.</summary>
    public IEnumerable<ResourceViewModel> Resources { get; set; } = Enumerable.Empty<ResourceViewModel>();
}