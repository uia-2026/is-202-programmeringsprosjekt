using Heimevernet.Infrastructure.Geo;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Models;
using Heimevernet.ViewModels.Match;

namespace Heimevernet.Mappers;

public class MatchCandidateMapper : IMatchCandidateMapper
{
    public MatchCandidateViewModel ToViewModel(
        Resource resource,
        double needLatitude,
        double needLongitude)
    {
        return new MatchCandidateViewModel
        {
            ResourceId = resource.Id,
            Title = resource.Title,
            Description = resource.Description,
            CategoryName = resource.Category.Name,
            Region = resource.Region,
            ContactPoint = resource.ContactPoint,
            QuantityOffered = resource.QuantityOffered,
            AvailableFrom = resource.AvailableFrom,
            AvailableTo = resource.AvailableTo,
            DistanceKm = GeoMath.DistanceKm(
                needLatitude,
                needLongitude,
                resource.Latitude,
                resource.Longitude)
        };
    }
}
