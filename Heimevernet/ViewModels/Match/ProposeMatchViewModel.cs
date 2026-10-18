namespace Heimevernet.ViewModels.Match;

/// <summary>
/// View model for the Propose-match picker page: shows the Need and lets
/// the Need's owner pick a candidate Resource to propose.
/// </summary>
public class ProposeMatchViewModel
{
    public int NeedId { get; set; }

    public string NeedTitle { get; set; } = string.Empty;

    public IEnumerable<MatchCandidateViewModel> Candidates { get; set; } = Enumerable.Empty<MatchCandidateViewModel>();
}