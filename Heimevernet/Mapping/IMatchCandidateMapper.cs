using Heimevernet.Models;
using Heimevernet.ViewModels.Match;
namespace Heimevernet.Mappers.Interfaces;

public interface IMatchCandidateMapper
{
    MatchCandidateViewModel ToViewModel(
        Resource resource,
        double needLatitude,
        double needLongitude);
}