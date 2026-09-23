using Heimevernet.Models;

namespace Heimevernet.ViewModels.Need;

public class NeedViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Region { get; set; } = string.Empty;

    public NeedPriority Priority { get; set; }

    public DateTime? Deadline { get; set; }

    public string ContactPoint { get; set; } = string.Empty;

    public NeedStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string FullAddress => Region;
}