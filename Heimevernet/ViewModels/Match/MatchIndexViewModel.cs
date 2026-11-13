namespace Heimevernet.ViewModels.Match;

/// <summary>
/// Model for the "My Matches" page.
/// </summary>
public class MatchIndexViewModel
{
    /// <summary>The user's matches, newest first.</summary>
    public IReadOnlyList<MatchListItemViewModel> Matches { get; set; } = [];

    // True when at least one row can be accepted or rejected by the current user.
    /// <summary><see langword="true"/> when at least one row can be accepted or rejected by the current user, so the actions column is shown.</summary>
    public bool ShowActions => Matches.Any(m => m.CanDecide);
}