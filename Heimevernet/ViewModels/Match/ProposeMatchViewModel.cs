using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Match;

/// <summary>
/// View model for the Propose-match picker page: shows the Need and lets
/// the Need's owner pick a candidate Resource to propose.
/// </summary>
public class ProposeMatchViewModel
{
    /// <summary>ID of the need to propose for.</summary>
    public int NeedId { get; set; }

    /// <summary>Title of the need, shown in the page heading.</summary>
    public string NeedTitle { get; set; } = string.Empty;

    /// <summary>Quantity to propose. Must be between 1 and <see cref="FieldLimits.MaxQuantity"/>. Defaults to 1.</summary>
    [Range(1, FieldLimits.MaxQuantity, ErrorMessage = "Quantity must be between 1 and 10000.")]
    public int Quantity { get; set; } = 1;

    /// <summary>Resources the user can pick from, nearest first.</summary>
    public IEnumerable<MatchCandidateViewModel> Candidates { get; set; } = Enumerable.Empty<MatchCandidateViewModel>();
}