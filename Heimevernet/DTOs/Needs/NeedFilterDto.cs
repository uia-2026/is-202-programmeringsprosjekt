using Heimevernet.Models;

namespace Heimevernet.DTOs.Needs;

/// <summary>
/// Optional filter and sort parameters for the Need list page.
/// Every field is optional — null means "no filter on this field."
/// </summary>
public class NeedFilterDto
{
    /// <summary>Only show needs with this status.</summary>
    public NeedStatus? Status { get; set; }
    /// <summary>Only show needs with this priority.</summary>
    public NeedPriority? Priority { get; set; }
    /// <summary>Only show needs in this category.</summary>
    public int? CategoryId { get; set; }
    /// <summary>Only show needs in this region (exact match).</summary>
    public string? Region { get; set; }
    /// <summary>Free text searched in title, description and region.</summary>
    public string? Search { get; set; }
    /// <summary>Sort order. Defaults to newest first.</summary>
    public NeedSortOption Sort { get; set; } = NeedSortOption.CreatedAtDescending;
}

/// <summary>
/// Sort options for the Need list page.
/// </summary>
public enum NeedSortOption
{
    /// <summary>Newest first.</summary>
    CreatedAtDescending,
    /// <summary>Oldest first.</summary>
    CreatedAtAscending,
    /// <summary>Highest priority first, then earliest deadline.</summary>
    PriorityDescending,
    /// <summary>Earliest deadline first.</summary>
    DeadlineAscending
}