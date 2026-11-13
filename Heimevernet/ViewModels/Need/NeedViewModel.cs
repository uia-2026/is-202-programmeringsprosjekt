using Heimevernet.Models;

namespace Heimevernet.ViewModels.Need;

/// <summary>
/// View model containing detailed information about a Need for presentation in views.
/// </summary>
public class NeedViewModel
{
    /// <summary>ID of the need.</summary>
    public int Id { get; set; }

    /// <summary>ID of the owner.</summary>
    public int UserId { get; set; }

    /// <summary><see langword="true"/> when the current user owns the need. Set by the controller; only used for display.</summary>
    public bool IsOwner { get; set; }

    /// <summary>Title of the need.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>ID of the category.</summary>
    public int CategoryId { get; set; }

    /// <summary>Latitude in decimal degrees.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude in decimal degrees.</summary>
    public double Longitude { get; set; }

    /// <summary>Region name.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>How urgent the need is.</summary>
    public NeedPriority Priority { get; set; }

    /// <summary>Deadline, or <see langword="null"/> for none.</summary>
    public DateTime? Deadline { get; set; }

    /// <summary>Contact details.</summary>
    public string ContactPoint { get; set; } = string.Empty;

    /// <summary>How many units are needed.</summary>
    public int QuantityNeeded { get; set; }

    /// <summary>Current status.</summary>
    public NeedStatus Status { get; set; }

    /// <summary>When the need was registered (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Location text for display. Currently the same as <see cref="Region"/>.</summary>
    public string FullAddress => Region;
}