using Heimevernet.Mappers.Interfaces;
using Heimevernet.Models;
using Heimevernet.ViewModels.Match;

namespace Heimevernet.Mappers;

public class MatchListMapper : IMatchListMapper
{
    public MatchListItemViewModel ToListItem(Match match, int currentUserId)
    {
        var isResourceOwner = match.Resource.UserId == currentUserId;

        return new MatchListItemViewModel
        {
            MatchId = match.Id,
            NeedId = match.NeedId,
            NeedTitle = match.Need.Title,
            ResourceId = match.ResourceId,
            ResourceTitle = match.Resource.Title,
            Quantity = match.Quantity,
            Status = match.Status,
            MatchedAt = match.MatchedAt,
            IsNeedOwner = match.Need.UserId == currentUserId,
            IsResourceOwner = isResourceOwner,
            CanDecide = isResourceOwner && match.Status == MatchStatus.UnderReview
        };
    }
}