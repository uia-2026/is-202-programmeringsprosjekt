using Heimevernet.Models;
using Heimevernet.ViewModels.Match;

namespace Heimevernet.Mappers.Interfaces;

/// <summary>
/// Maps a match to a row on the "My Matches" page.
/// </summary>
public interface IMatchListMapper
{
    /// <summary>
    /// Maps a match to a list row and sets the ownership flags for the current user.
    /// </summary>
    /// <param name="match">The match, with its need and resource loaded.</param>
    /// <param name="currentUserId">ID of the signed-in user.</param>
    /// <param name="needLatitude">Latitude of the need.</param>
    /// <param name="needLongitude">Longitude of the need.</param>
    /// <param name="resourceLatitude">Latitude of the resource.</param>
    /// <param name="resourceLongitude">Longitude of the resource.</param>
    /// <returns>The row as a <see cref="MatchListItemViewModel"/>.</returns>
    MatchListItemViewModel ToListItem(Match match, int currentUserId, double needLatitude, double needLongitude, double resourceLatitude, double resourceLongitude);
}