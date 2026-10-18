using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Match;

namespace Heimevernet.Services;

/// <summary>
/// Business logic for the Match: Propose flow (#40).
/// Suggests candidate Resources for a Need and creates a Match when a
/// Public Actor proposes one, with server-side validation.
/// </summary>
public class MatchService : IMatchService
{
    private readonly INeedRepository _needRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MatchService(
        INeedRepository needRepository,
        IResourceRepository resourceRepository,
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork)
    {
        _needRepository = needRepository;
        _resourceRepository = resourceRepository;
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<MatchCandidateViewModel>> GetCandidateResourcesAsync(int needId, CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdAsync(needId, cancellationToken);
        if (need is null)
        {
            return Enumerable.Empty<MatchCandidateViewModel>();
        }

        var targetDate = need.Deadline ?? DateTime.UtcNow;

        var resources = await _resourceRepository.GetAllAsync(cancellationToken);

        var candidates = resources
            .Where(r => r.CategoryId == need.CategoryId)
            .Where(r => r.Status == ResourceStatus.Available)
            .Where(r => r.AvailableFrom <= targetDate && (r.AvailableTo == null || r.AvailableTo >= targetDate))
            .Select(r => new MatchCandidateViewModel
            {
                ResourceId = r.Id,
                Title = r.Title,
                Description = r.Description,
                CategoryName = r.Category.Name,
                Region = r.Region,
                ContactPoint = r.ContactPoint,
                AvailableFrom = r.AvailableFrom,
                AvailableTo = r.AvailableTo,
                DistanceKm = DistanceKm(need.Latitude, need.Longitude, r.Latitude, r.Longitude)
            })
            .OrderBy(c => c.DistanceKm)
            .ToList();

        return candidates;
    }

    public async Task<bool> CanProposeAsync(int needId, int userId, CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdAsync(needId, cancellationToken);
        return need is not null && need.UserId == userId;
    }

    public async Task<(ProposeMatchResult Result, int? MatchId)> ProposeAsync(int needId, int resourceId, int userId, CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdAsync(needId, cancellationToken);
        if (need is null)
        {
            return (ProposeMatchResult.NeedNotFound, null);
        }

        if (need.UserId != userId)
        {
            return (ProposeMatchResult.NotNeedOwner, null);
        }

        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken);
        if (resource is null)
        {
            return (ProposeMatchResult.ResourceNotFound, null);
        }

        if (resource.Status != ResourceStatus.Available)
        {
            return (ProposeMatchResult.ResourceNotAvailable, null);
        }

        var alreadyMatched = await _matchRepository.ExistsAsync(needId, resourceId, cancellationToken);
        if (alreadyMatched)
        {
            return (ProposeMatchResult.DuplicateMatch, null);
        }

        var match = new Match
        {
            NeedId = needId,
            ResourceId = resourceId,
            MatchedByUserId = userId
        };

        await _matchRepository.AddAsync(match, cancellationToken);

        need.Status = NeedStatus.UnderReview;

        await _unitOfWork.CommitAsync();

        return (ProposeMatchResult.Success, match.Id);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}