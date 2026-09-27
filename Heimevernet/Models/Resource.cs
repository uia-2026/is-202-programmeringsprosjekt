namespace Heimevernet.Models;

/// <summary>
/// Represents a physical resource that can be assigned to a need (for example a vehicle, team or equipment).
/// </summary>
public class Resource
{
    /// <summary>Database identity for the resource.</summary>
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Latitude (WGS84) for the resource location.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude (WGS84) for the resource location.</summary>
    public double Longitude { get; set; }

    public string Region { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    /// <summary>Timestamp when the resource becomes available.</summary>
    public DateTime AvailableFrom { get; set; }

    public DateTime? AvailableTo { get; set; }

    public string ContactPoint { get; set; } = string.Empty;

    public ResourceStatus Status { get; set; } = ResourceStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}