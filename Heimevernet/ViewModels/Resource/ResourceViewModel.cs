using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

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
}