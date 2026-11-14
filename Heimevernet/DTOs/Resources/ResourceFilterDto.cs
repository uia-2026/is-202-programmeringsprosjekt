using Heimevernet.Models;

namespace Heimevernet.DTOs.Resources;

/// <summary>
/// Optional filter and sort parameters for the Resource list page.
/// Every field is optional — null means "no filter on this field."
/// </summary>
public class ResourceFilterDto
{
    /// <summary>Only show resources with this status.</summary>
    public ResourceStatus? Status { get; set; }
    /// <summary>Only show resources in this category.</summary>
    public int? CategoryId { get; set; }
    /// <summary>Only show resources in this region (exact match).</summary>
    public string? Region { get; set; }
    /// <summary>Only show resources owned by this user. Always set to the current user for resource providers.</summary>
    public int? OwnerUserId { get; set; }
    /// <summary>When true, only show resources owned by the signed-in user.</summary>
    public bool ShowOnlyMine { get; set; }
    /// <summary>
    /// Only include resources available during this window (inclusive).
    /// A resource matches if its own AvailableFrom/AvailableTo overlaps this range.
    /// Either end may be left blank.
    /// </summary>
    public DateTime? AvailableFrom { get; set; }
    /// <summary>End of the availability window. See <see cref="AvailableFrom"/>.</summary>
    public DateTime? AvailableTo { get; set; }

    /// <summary>Free text to search for.</summary>
    public string? Search { get; set; }
    /// <summary>Sort order. Defaults to newest first.</summary>
    public ResourceSortOption Sort { get; set; } = ResourceSortOption.CreatedAtDescending;
}

/// <summary>
/// Sort options for the Resource list page.
/// </summary>
public enum ResourceSortOption
{
    /// <summary>Newest first.</summary>
    CreatedAtDescending,
    /// <summary>Oldest first.</summary>
    CreatedAtAscending,
    /// <summary>Soonest available first.</summary>
    AvailableFromAscending
}