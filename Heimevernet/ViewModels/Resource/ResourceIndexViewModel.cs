using Heimevernet.DTOs.Resources;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model for the filterable Resource list page (#63).
/// </summary>
public class ResourceIndexViewModel
{
    /// <summary>The current filter values, used to fill in the filter form.</summary>
    public ResourceFilterDto Filter { get; set; } = new();

    /// <summary>The resources that match the filter.</summary>
    public IEnumerable<ResourceViewModel> Items { get; set; } = Enumerable.Empty<ResourceViewModel>();

    /// <summary>Categories for the category filter.</summary>
    public IEnumerable<Category> AvailableCategories { get; set; } = Enumerable.Empty<Category>();

    /// <summary>Regions for the region filter.</summary>
    public IEnumerable<string> AvailableRegions { get; set; } = Enumerable.Empty<string>();
}