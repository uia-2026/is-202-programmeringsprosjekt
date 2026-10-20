namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model for the print-friendly, region-filtered Resource list (#41).
/// </summary>
public class PrintByRegionViewModel
{
    public string? SelectedRegion { get; set; }

    public IEnumerable<string> AvailableRegions { get; set; } = Enumerable.Empty<string>();

    public IEnumerable<ResourceViewModel> Resources { get; set; } = Enumerable.Empty<ResourceViewModel>();
}