using Heimevernet.Models;
using Heimevernet.ViewModels.Attachment;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model containing detailed information about a Resource for presentation in views.
/// </summary>
public class ResourceViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Region { get; set; } = string.Empty;

    public DateTime AvailableFrom { get; set; }

    public DateTime? AvailableTo { get; set; }

    public string ContactPoint { get; set; } = string.Empty;

    public ResourceStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }
    public bool IsOwner { get; set; }
    public IReadOnlyList<AttachmentViewModel> Attachments { get; set; } = Array.Empty<AttachmentViewModel>();
}