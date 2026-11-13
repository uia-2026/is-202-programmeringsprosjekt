using Heimevernet.Models;

namespace Heimevernet.ViewModels.Match;

/// <summary>
/// One row on the "My Matches" page.
/// </summary>
public class MatchListItemViewModel
{
    /// <summary>ID of the match.</summary>
    public int MatchId { get; set; }

    /// <summary>ID of the need in the match.</summary>
    public int NeedId { get; set; }

    /// <summary>Title of the need.</summary>
    public string NeedTitle { get; set; } = string.Empty;

    /// <summary>ID of the resource in the match.</summary>
    public int ResourceId { get; set; }

    /// <summary>Title of the resource.</summary>
    public string ResourceTitle { get; set; } = string.Empty;

    /// <summary>Quantity allocated by the match.</summary>
    public int Quantity { get; set; }
    /// <summary>Distance between the need and the resource, in kilometres.</summary>
    public double DistanceKm { get; set; }
    /// <summary>Current status of the match.</summary>
    public MatchStatus Status { get; set; }

    /// <summary>When the match was proposed (UTC).</summary>
    public DateTime MatchedAt { get; set; }

    // True when the current user owns the Need in this match.
    /// <summary><see langword="true"/> when the current user owns the need.</summary>
    public bool IsNeedOwner { get; set; }

    // True when the current user owns the Resource in this match.
    /// <summary><see langword="true"/> when the current user owns the resource.</summary>
    public bool IsResourceOwner { get; set; }

    // True when the current user owns the Resource and the match is UnderReview.
    // Only controls whether Accept/Reject buttons are shown; MatchService enforces ownership.
    /// <summary>
    /// <see langword="true"/> when the current user owns the resource and the match is under review.
    /// Only controls the Accept/Reject buttons; the match service still checks ownership.
    /// </summary>
    public bool CanDecide { get; set; }
}