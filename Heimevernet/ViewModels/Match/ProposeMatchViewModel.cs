using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Match;

/// <summary>
/// View model for the Propose-match picker page: shows the Need and lets
/// the Need's owner pick a candidate Resource to propose.
/// </summary>
public class ProposeMatchViewModel
{
    public int NeedId { get; set; }

    public string NeedTitle { get; set; } = string.Empty;

    [Range(1, FieldLimits.MaxQuantity, ErrorMessage = "Quantity must be between 1 and 10000.")]
    public int Quantity { get; set; } = 1;

    public IEnumerable<MatchCandidateViewModel> Candidates { get; set; } = Enumerable.Empty<MatchCandidateViewModel>();
}