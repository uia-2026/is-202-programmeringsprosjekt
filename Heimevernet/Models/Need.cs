using Heimevernet.Models.Interfaces;

namespace Heimevernet.Models;


/// <summary>
/// Domain model representing a reported need.
/// Contains metadata such as category, location, priority, status and related matches.
/// </summary>
public class Need : IVersioned
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>ID of the user who registered the need (the owner).</summary>
    public int UserId { get; set; }
    /// <summary>The user who registered the need.</summary>
    public User User { get; set; } = null!;

    /// <summary>ID of the need's category.</summary>
    public int CategoryId { get; set; }
    /// <summary>The need's category.</summary>
    public Category Category { get; set; } = null!;

    /// <summary>Short title shown in lists. Max <see cref="FieldLimits.Title"/> characters.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description. Max <see cref="FieldLimits.Description"/> characters.</summary>
    public string? Description { get; set; }

    /// <summary>Latitude of the need's location, in decimal degrees.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude of the need's location, in decimal degrees.</summary>
    public double Longitude { get; set; }

    /// <summary>Region (county) found from the location, or "International".</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>How urgent the need is.</summary>
    public NeedPriority Priority { get; set; }

    /// <summary>When the need must be filled by, or <see langword="null"/> for no deadline.</summary>
    public DateTime? Deadline { get; set; }

    /// <summary>Who to contact about the need, e.g. a phone number or name.</summary>
    public string ContactPoint { get; set; } = string.Empty;

    /// <summary>Where the need is in its life cycle. Starts as <see cref="NeedStatus.New"/>.</summary>
    public NeedStatus Status { get; set; } = NeedStatus.New;

    /// <summary>How many units are needed. At least 1 (database check constraint).</summary>
    public int QuantityNeeded { get; set; } = 1;

    /// <summary>When the need was registered (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Matches proposed for this need.</summary>
    public ICollection<Match> Matches { get; set; } = [];

    // Concurrency token for optimistic concurrency control.
    /// <inheritdoc />
    public int Version { get; set; }
}