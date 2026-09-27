using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// Lightweight view model for displaying a summary of a Resource in lists.
/// Contains selected fields such as Title, CategoryName and Region.
/// </summary>
public class ResourceSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public NeedPriority Priority { get; set; }
    public NeedStatus Status { get; set; }
}