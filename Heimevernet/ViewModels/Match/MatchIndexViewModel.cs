namespace Heimevernet.ViewModels.Match;

/// <summary>
/// Model for the "My Matches" page.
/// </summary>
public class MatchIndexViewModel
{
    public bool IsResourceProvider { get; set; }

    public IReadOnlyList<MatchListItemViewModel> Matches { get; set; } = [];
}