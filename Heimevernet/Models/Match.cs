using Heimevernet.Models.Interfaces;

namespace Heimevernet.Models;

/// <summary>
/// Represents a pairing between a Need and a Resource.
/// Contains status and metadata about which user created the match.
/// </summary>
public class Match : IVersioned
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>ID of the need being filled.</summary>
    public int NeedId { get; set; }
    /// <summary>The need being filled.</summary>
    public Need Need { get; set; } = null!;

    /// <summary>ID of the resource that is offered.</summary>
    public int ResourceId { get; set; }
    /// <summary>The resource that is offered.</summary>
    public Resource Resource { get; set; } = null!;

    /// <summary>Where the match is in its life cycle. Starts as <see cref="MatchStatus.UnderReview"/>.</summary>
    public MatchStatus Status { get; set; } = MatchStatus.UnderReview;

    /// <summary>ID of the user who proposed the match (the need owner).</summary>
    public int MatchedByUserId { get; set; }
    /// <summary>The user who proposed the match.</summary>
    public User MatchedByUser { get; set; } = null!;

    /// <summary>How many units of the resource are allocated to the need. At least 1.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>When the match was proposed (UTC).</summary>
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;

    // Concurrency token for optimistic concurrency control
    /// <inheritdoc />
    public int Version { get; set; }
}