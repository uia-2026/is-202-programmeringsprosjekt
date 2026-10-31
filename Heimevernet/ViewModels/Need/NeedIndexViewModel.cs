using Heimevernet.DTOs.Needs;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Need;

/// <summary>
/// View model for the filterable Need list page (#63).
/// </summary>
public class NeedIndexViewModel
{
    public NeedFilterDto Filter { get; set; } = new();

    public IEnumerable<NeedViewModel> Items { get; set; } = Enumerable.Empty<NeedViewModel>();

    public IEnumerable<Category> AvailableCategories { get; set; } = Enumerable.Empty<Category>();

    public IEnumerable<string> AvailableRegions { get; set; } = Enumerable.Empty<string>();
}