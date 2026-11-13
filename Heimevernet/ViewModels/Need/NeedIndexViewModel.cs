using Heimevernet.DTOs.Needs;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Need;

/// <summary>
/// View model for the filterable Need list page (#63).
/// </summary>
public class NeedIndexViewModel
{
    /// <summary>The current filter values, used to fill in the filter form.</summary>
    public NeedFilterDto Filter { get; set; } = new();

    /// <summary>The needs that match the filter.</summary>
    public IEnumerable<NeedViewModel> Items { get; set; } = Enumerable.Empty<NeedViewModel>();

    /// <summary>Categories for the category filter.</summary>
    public IEnumerable<Category> AvailableCategories { get; set; } = Enumerable.Empty<Category>();

    /// <summary>Regions for the region filter.</summary>
    public IEnumerable<string> AvailableRegions { get; set; } = Enumerable.Empty<string>();
}