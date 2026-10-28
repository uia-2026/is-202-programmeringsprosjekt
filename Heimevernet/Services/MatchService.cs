using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Match;
using Heimevernet.Exceptions;
using Heimevernet.Mappers.Interfaces;

namespace Heimevernet.Services;

/// <summary>
/// Business logic for the Match flow.
/// Propose (#40): suggests candidate Resources for a Need and creates a Match.
/// My Matches: lists matches and lets the Resource Provider accept or reject them.
/// </summary>
public class MatchService : IMatchService
{
    private readonly INeedRepository _needRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMatchCandidateMapper _matchCandidateMapper;
    private readonly IMatchListMapper _matchListMapper; // NYTT I STEG 7

    public MatchService(
        INeedRepository needRepository,
        IResourceRepository resourceRepository,
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        IMatchCandidateMapper matchCandidateMapper,
        IMatchListMapper matchListMapper) // NYTT I STEG 7
    {
        _needRepository = needRepository;
        _resourceRepository = resourceRepository;
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _matchCandidateMapper = matchCandidateMapper;
        _matchListMapper = matchListMapper; // NYTT I STEG 7
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

        var acceptedNeedAllocation = await _matchRepository
            .GetAcceptedAllocatedQuantityForNeedAsync(needId, cancellationToken);

        var acceptedResourceAllocation = await _matchRepository
            .GetAcceptedAllocatedQuantityForResourceAsync(resourceId, cancellationToken);

        var needRemaining = Math.Max(0, need.QuantityNeeded - acceptedNeedAllocation);
        var resourceRemaining = Math.Max(0, resource.QuantityOffered - acceptedResourceAllocation);
        var maxAllowed = Math.Min(needRemaining, resourceRemaining);

        if (quantity > maxAllowed)
        {
            return (ProposeMatchResult.InvalidQuantity, null);
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
        _resourceRepository.MarkForConcurrencyCheck(resource);

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

    // ---------- NYTT I STEG 7 ----------

    public async Task<MatchIndexViewModel> GetMyMatchesAsync(
        int userId,
        bool isResourceProvider,
        CancellationToken cancellationToken = default)
    {
        var matches = isResourceProvider
            ? await _matchRepository.GetForResourceOwnerAsync(userId, cancellationToken)
            : await _matchRepository.GetForNeedOwnerAsync(userId, cancellationToken);

        return new MatchIndexViewModel
        {
            IsResourceProvider = isResourceProvider,
            Matches = matches
                .Select(m => _matchListMapper.ToListItem(m, isResourceProvider))
                .ToList()
        };
    }

    public async Task<MatchDecisionResult> AcceptAsync(
        int matchId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var match = await _matchRepository.GetByIdWithNeedAndResourceAsync(matchId, cancellationToken);

        if (match is null)
        {
            return MatchDecisionResult.MatchNotFound;
        }

        if (match.Resource.UserId != userId)
        {
            return MatchDecisionResult.UserIsNotResourceOwner;
        }

        if (match.Status != MatchStatus.UnderReview)
        {
            return MatchDecisionResult.MatchNotUnderReview;
        }

        // How much is already given away (accepted) on this need and this resource?
        var acceptedNeedAllocation = await _matchRepository
            .GetAcceptedAllocatedQuantityForNeedAsync(match.NeedId, cancellationToken);

        var acceptedResourceAllocation = await _matchRepository
            .GetAcceptedAllocatedQuantityForResourceAsync(match.ResourceId, cancellationToken);

        var needRemaining = match.Need.QuantityNeeded - acceptedNeedAllocation;
        var resourceRemaining = match.Resource.QuantityOffered - acceptedResourceAllocation;

        if (match.Quantity > needRemaining || match.Quantity > resourceRemaining)
        {
            return MatchDecisionResult.InsufficientQuantity;
        }

        match.Status = MatchStatus.Assigned;

        // Force a version check on Need and Resource, so two people cannot
        // accept matches on the same need or resource at the same time.
        _needRepository.MarkForConcurrencyCheck(match.Need);
        _resourceRepository.MarkForConcurrencyCheck(match.Resource);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return MatchDecisionResult.ConcurrentChange;
        }

        return MatchDecisionResult.Success;
    }

    public async Task<MatchDecisionResult> RejectAsync(
        int matchId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var match = await _matchRepository.GetByIdWithNeedAndResourceAsync(matchId, cancellationToken);

        if (match is null)
        {
            return MatchDecisionResult.MatchNotFound;
        }

        if (match.Resource.UserId != userId)
        {
            return MatchDecisionResult.UserIsNotResourceOwner;
        }

        if (match.Status != MatchStatus.UnderReview)
        {
            return MatchDecisionResult.MatchNotUnderReview;
        }

        _matchRepository.Remove(match);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return MatchDecisionResult.ConcurrentChange;
        }

        return MatchDecisionResult.Success;
    }
}