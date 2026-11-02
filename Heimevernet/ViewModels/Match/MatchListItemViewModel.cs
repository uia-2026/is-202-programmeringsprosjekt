using Heimevernet.Models;

namespace Heimevernet.ViewModels.Match;

/// <summary>
/// One row on the "My Matches" page.
/// </summary>
public class MatchListItemViewModel
{
    public int MatchId { get; set; }

    public int NeedId { get; set; }

    public string NeedTitle { get; set; } = string.Empty;

    public int ResourceId { get; set; }

    public string ResourceTitle { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public MatchStatus Status { get; set; }

    public DateTime MatchedAt { get; set; }

    // True when the current user owns the Need in this match.
    public bool IsNeedOwner { get; set; }

    // True when the current user owns the Resource in this match.
    public bool IsResourceOwner { get; set; }

    // True when the current user owns the Resource and the match is UnderReview.
    // Only controls whether Accept/Reject buttons are shown; MatchService enforces ownership.
    public bool CanDecide { get; set; }
}