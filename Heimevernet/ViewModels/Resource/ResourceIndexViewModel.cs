using Heimevernet.DTOs.Resources;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model for the filterable Resource list page (#63).
/// </summary>
public class ResourceIndexViewModel
{
    public ResourceFilterDto Filter { get; set; } = new();

    public IEnumerable<ResourceViewModel> Items { get; set; } = Enumerable.Empty<ResourceViewModel>();

    public IEnumerable<Category> AvailableCategories { get; set; } = Enumerable.Empty<Category>();

    public IEnumerable<string> AvailableRegions { get; set; } = Enumerable.Empty<string>();
}