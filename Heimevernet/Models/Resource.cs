using Heimevernet.Models.Interfaces;

namespace Heimevernet.Models;

/// <summary>
/// Domain model representing an available resource.
/// Includes availability window, location, category and related attachments/matches.
/// </summary>
public class Resource : IVersioned
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>ID of the user who registered the resource (the owner).</summary>
    public int UserId { get; set; }
    /// <summary>The user who registered the resource.</summary>
    public User User { get; set; } = null!;

    /// <summary>ID of the resource's category.</summary>
    public int CategoryId { get; set; }
    /// <summary>The resource's category.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Short title shown in lists. Max <see cref="FieldLimits.Title"/> characters.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description. Max <see cref="FieldLimits.Description"/> characters.</summary>
    public string? Description { get; set; }

    /// <summary>Latitude of the resource's location, in decimal degrees.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude of the resource's location, in decimal degrees.</summary>
    public double Longitude { get; set; }

    /// <summary>Region (county) found from the location, or "International".</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>When the resource becomes available.</summary>
    public DateTime AvailableFrom { get; set; }

    /// <summary>When the resource stops being available, or <see langword="null"/> for no end date.</summary>
    public DateTime? AvailableTo { get; set; }

    /// <summary>Who to contact about the resource, e.g. a phone number or name.</summary>
    public string ContactPoint { get; set; } = string.Empty;

    /// <summary>Whether the resource can be proposed right now.</summary>
    public ResourceStatus Status { get; set; } = ResourceStatus.Available;

    /// <summary>How many units are offered. At least 1.</summary>
    public int QuantityOffered { get; set; } = 1;

    /// <summary>When the resource was registered (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Photos of the resource.</summary>
    public ICollection<Attachment> Attachments { get; set; } = [];
    /// <summary>Matches where this resource has been proposed.</summary>
    public ICollection<Match> Matches { get; set; } = [];

    // Concurrency token for optimistic concurrency control.
    /// <inheritdoc />
    public int Version { get; set; }
}