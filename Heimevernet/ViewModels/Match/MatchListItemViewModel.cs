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

    // True only for the Resource Provider when the match is UnderReview.
    public bool CanDecide { get; set; }
}