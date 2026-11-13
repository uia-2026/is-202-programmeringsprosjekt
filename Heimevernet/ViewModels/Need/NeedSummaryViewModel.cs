using Heimevernet.Models;

namespace Heimevernet.ViewModels.Need;

/// <summary>
/// Lightweight view model for displaying a summary of a Need in lists.
/// Contains selected fields such as Title, CategoryName, Region, Priority and Status.
/// </summary>
public class NeedSummaryViewModel
{
    /// <summary>ID of the need.</summary>
    public int Id { get; set; }
    /// <summary>Title of the need.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;
    /// <summary>Region name.</summary>
    public string Region { get; set; } = string.Empty;
    /// <summary>How many units are needed.</summary>
    public int QuantityNeeded { get; set; }
    /// <summary>How urgent the need is.</summary>
    public NeedPriority Priority { get; set; }
    /// <summary>Current status of the need.</summary>
    public NeedStatus Status { get; set; }
}