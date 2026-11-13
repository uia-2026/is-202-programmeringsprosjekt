using Heimevernet.Models;
using Heimevernet.ViewModels.Attachment;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model containing detailed information about a Resource for presentation in views.
/// </summary>
public class ResourceViewModel
{
    /// <summary>ID of the resource.</summary>
    public int Id { get; set; }

    /// <summary>Title of the resource.</summary>
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

    /// <summary>When the resource becomes available.</summary>
    public DateTime AvailableFrom { get; set; }

    /// <summary>When the resource stops being available, or <see langword="null"/>.</summary>
    public DateTime? AvailableTo { get; set; }

    /// <summary>Contact details.</summary>
    public string ContactPoint { get; set; } = string.Empty;

    /// <summary>How many units are offered.</summary>
    public int QuantityOffered { get; set; }

    /// <summary>Current status.</summary>
    public ResourceStatus Status { get; set; }

    /// <summary>When the resource was registered (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>ID of the owner.</summary>
    public int UserId { get; set; }
    /// <summary><see langword="true"/> when the current user owns the resource. Set by the controller; only used for display.</summary>
    public bool IsOwner { get; set; }
    /// <summary>Photos of the resource. Filled on the details page only.</summary>
    public IReadOnlyList<AttachmentViewModel> Attachments { get; set; } = Array.Empty<AttachmentViewModel>();
}