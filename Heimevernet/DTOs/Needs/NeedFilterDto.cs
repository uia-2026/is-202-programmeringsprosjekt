using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

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
    /// <summary>Set server-side from the logged-in user; never bound from the request.</summary>
    [BindNever]
    public int? OwnerUserId { get; set; }
    public bool ShowOnlyMine { get; set; }
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