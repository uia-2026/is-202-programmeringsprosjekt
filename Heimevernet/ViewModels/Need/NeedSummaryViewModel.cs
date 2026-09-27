using Heimevernet.Models;

namespace Heimevernet.ViewModels.Need;

/// <summary>
/// Lightweight view model for displaying a summary of a Need in lists.
/// Contains selected fields such as Title, CategoryName, Region, Priority and Status.
/// </summary>
public class NeedSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public NeedPriority Priority { get; set; }
    public NeedStatus Status { get; set; }
}