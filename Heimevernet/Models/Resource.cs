namespace Heimevernet.Models;

/// <summary>
/// Represents a physical resource that can be assigned to a need (for example a vehicle, team or equipment).
/// </summary>
public class Resource
{
    /// <summary>Database identity for the resource.</summary>
    public int Id { get; set; }

    /// <summary>Type or category of the resource (e.g. "Ambulance", "Generator").</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Latitude (WGS84) for the resource location.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude (WGS84) for the resource location.</summary>
    public double Longitude { get; set; }

    /// <summary>Timestamp when the resource becomes available.</summary>
    public DateTime AvailableFrom { get; set; }

    /// <summary>Primary contact name for the resource.</summary>
    public string ContactName { get; set; } = string.Empty;

    /// <summary>Contact information (phone or email) for the resource.</summary>
    public string ContactInfo { get; set; } = string.Empty;

    /// <summary>Current status in the resource lifecycle.</summary>
    public ResourceStatus Status { get; set; } = ResourceStatus.New;

    /// <summary>Moves the resource to the next lifecycle status.</summary>
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

/// <summary>
/// Lifecycle states for a <see cref="Resource"/>.
/// </summary>
public enum ResourceStatus
{
    /// <summary>Resource is newly created and has not been reviewed.</summary>
    New,
    /// <summary>Resource is under review and not yet assigned.</summary>
    UnderReview,
    /// <summary>Resource has been assigned to a need.</summary>
    Assigned,
    /// <summary>Resource has been resolved or released.</summary>
    Resolved
}
