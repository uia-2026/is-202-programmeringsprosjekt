using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// Lightweight view model for displaying a summary of a Resource in lists.
/// Contains selected fields such as Title, CategoryName and Region.
/// </summary>
public class ResourceSummaryViewModel
{
    /// <summary>ID of the resource.</summary>
    public int Id { get; set; }
    /// <summary>Title of the resource.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;
    /// <summary>Region name.</summary>
    public string Region { get; set; } = string.Empty;
    /// <summary>How many units are offered.</summary>
    public int QuantityOffered { get; set; }
}