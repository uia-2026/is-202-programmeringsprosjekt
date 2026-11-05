using Heimevernet.Models;
using Heimevernet.ViewModels.Match;

namespace Heimevernet.Mappers.Interfaces;

public interface IMatchListMapper
{
    MatchListItemViewModel ToListItem(Match match, int currentUserId, double needLatitude, double needLongitude, double resourceLatitude, double resourceLongitude);
} 
