using Heimevernet.Models;
using Heimevernet.ViewModels.Match;
namespace Heimevernet.Mappers.Interfaces;

/// <summary>
/// Maps a resource to a candidate row on the Propose page.
/// </summary>
public interface IMatchCandidateMapper
{
    /// <summary>
    /// Maps a resource to a candidate and works out its distance from the need.
    /// </summary>
    /// <param name="resource">The resource, with its category loaded.</param>
    /// <param name="needLatitude">Latitude of the need.</param>
    /// <param name="needLongitude">Longitude of the need.</param>
    /// <returns>The candidate as a <see cref="MatchCandidateViewModel"/>.</returns>
    MatchCandidateViewModel ToViewModel(
        Resource resource,
        double needLatitude,
        double needLongitude);
}