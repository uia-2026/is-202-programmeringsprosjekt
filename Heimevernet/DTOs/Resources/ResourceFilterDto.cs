using Heimevernet.Models;

namespace Heimevernet.DTOs.Resources;

/// <summary>
/// Optional filter and sort parameters for the Resource list page.
/// Every field is optional — null means "no filter on this field."
/// </summary>
public class ResourceFilterDto
{
    public ResourceStatus? Status { get; set; }
    public int? CategoryId { get; set; }
    public string? Region { get; set; }
    public int? OwnerUserId { get; set; }
    /// <summary>
    /// Only include resources available during this window (inclusive).
    /// A resource matches if its own AvailableFrom/AvailableTo overlaps this range.
    /// Either end may be left blank.
    /// </summary>
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }

    public string? Search { get; set; }
    public ResourceSortOption Sort { get; set; } = ResourceSortOption.CreatedAtDescending;
}

/// <summary>
/// Sort options for the Resource list page.
/// </summary>
public enum ResourceSortOption
{
    CreatedAtDescending,
    CreatedAtAscending,
    AvailableFromAscending
}