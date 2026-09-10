namespace Heimevernet.Models;

public class Resource
{
    public int Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime AvailableFrom { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string ContactInfo { get; set; } = string.Empty;

    public ResourceStatus Status { get; set; } = ResourceStatus.New;

    public void MoveToNextStatus()
    {
        Status = Status switch
        {
            ResourceStatus.New => ResourceStatus.UnderReview,
            ResourceStatus.UnderReview => ResourceStatus.Assigned,
            ResourceStatus.Assigned => ResourceStatus.Resolved,
            _ => throw new InvalidOperationException($"Cannot advance status from {Status}")
        };
    }
}

public enum ResourceStatus
{
    New,
    UnderReview,
    Assigned,
    Resolved
}
