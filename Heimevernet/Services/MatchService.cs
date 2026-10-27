using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Match;
using Heimevernet.Exceptions;
using Heimevernet.Mappers.Interfaces;

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
    private readonly IMatchCandidateMapper _matchCandidateMapper;

    public MatchService(
        INeedRepository needRepository,
        IResourceRepository resourceRepository,
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        IMatchCandidateMapper matchCandidateMapper)
    {
        _needRepository = needRepository;
        _resourceRepository = resourceRepository;
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _matchCandidateMapper = matchCandidateMapper;
    }

    public async Task<IReadOnlyList<MatchCandidateViewModel>> GetCandidateResourcesAsync(
        int needId,
        CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdAsync(needId, cancellationToken);

        if (need is null || need.Status is not (NeedStatus.New or NeedStatus.UnderReview))
        {
            return Array.Empty<MatchCandidateViewModel>();
        }

        var resources = await _resourceRepository.GetCandidateResourcesAsync(
            need.Id, need.CategoryId, need.Deadline, DateTime.UtcNow, cancellationToken);

        return resources
            .Select(r => _matchCandidateMapper.ToViewModel(
                r, need.Latitude, need.Longitude))
            .OrderBy(c => c.DistanceKm)
            .ThenBy(c => c.ResourceId)
            .ToList();
    }

    public async Task<bool> CanProposeAsync(int needId, int userId, CancellationToken cancellationToken = default)
    {
        var need = await _needRepository.GetByIdAsync(needId, cancellationToken);
        return need is not null && need.UserId == userId;
    }

    public async Task<(ProposeMatchResult Result, int? MatchId)> ProposeAsync(
        int needId,
        int resourceId,
        int quantity,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 1 || quantity > FieldLimits.MaxQuantity)
        {
            return (ProposeMatchResult.InvalidQuantity, null);
        }

        var need = await _needRepository.GetByIdAsync(needId, cancellationToken);

        if (need is null)
        {
            return (ProposeMatchResult.NeedNotFound, null);
        }

        if (need.UserId != userId)
        {
            return (ProposeMatchResult.UserIsNotNeedOwner, null);
        }

        if (need.Status is not (NeedStatus.New or NeedStatus.UnderReview))
        {
            return (ProposeMatchResult.NeedNotOpen, null);
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

        var alreadyMatched = await _matchRepository.ExistsAsync(
            needId,
            resourceId,
            cancellationToken);

        if (alreadyMatched)
        {
            return (ProposeMatchResult.MatchAlreadyExists, null);
        }

        var match = new Match
        {
            NeedId = needId,
            ResourceId = resourceId,
            MatchedByUserId = userId,
            Quantity = quantity
        };

        await _matchRepository.AddAsync(match, cancellationToken);

        need.Status = NeedStatus.UnderReview;

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return (ProposeMatchResult.MatchAlreadyExists, null);
        }
        catch (ConcurrencyConflictException)
        {
            return (ProposeMatchResult.ConcurrentChange, null);
        }

        return (ProposeMatchResult.Success, match.Id);
    }
}