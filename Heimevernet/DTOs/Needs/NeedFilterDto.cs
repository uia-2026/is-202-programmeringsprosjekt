using Heimevernet.Models;

namespace Heimevernet.DTOs.Needs;

/// <summary>
/// Optional filter and sort parameters for the Need list page.
/// Every field is optional — null means "no filter on this field."
/// </summary>
public class NeedFilterDto
{
    public NeedStatus? Status { get; set; }
    public NeedPriority? Priority { get; set; }
    public int? CategoryId { get; set; }
    public string? Region { get; set; }
    public string? Search { get; set; }
    public NeedSortOption Sort { get; set; } = NeedSortOption.CreatedAtDescending;
}

/// <summary>
/// Sort options for the Need list page.
/// </summary>
public enum NeedSortOption
{
    CreatedAtDescending,
    CreatedAtAscending,
    PriorityDescending,
    DeadlineAscending
}