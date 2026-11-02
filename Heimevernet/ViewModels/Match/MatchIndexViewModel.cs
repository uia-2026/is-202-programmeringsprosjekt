namespace Heimevernet.ViewModels.Match;

/// <summary>
/// Model for the "My Matches" page.
/// </summary>
public class MatchIndexViewModel
{
    public IReadOnlyList<MatchListItemViewModel> Matches { get; set; } = [];

    // True when at least one row can be accepted or rejected by the current user.
    public bool ShowActions => Matches.Any(m => m.CanDecide);
}