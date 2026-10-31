using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Match;
using Heimevernet.Exceptions;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Domain.Rules;

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
    private readonly IMatchListMapper _matchListMapper;

    public MatchService(
        INeedRepository needRepository,
        IResourceRepository resourceRepository,
        IMatchRepository matchRepository,
        IUnitOfWork unitOfWork,
        IMatchCandidateMapper matchCandidateMapper,
        IMatchListMapper matchListMapper)
    {
        _needRepository = needRepository;
        _resourceRepository = resourceRepository;
        _matchRepository = matchRepository;
        _unitOfWork = unitOfWork;
        _matchCandidateMapper = matchCandidateMapper;
        _matchListMapper = matchListMapper;
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

    public async Task<(ProposeMatchResult Result, int? MatchId, int? MaxAllowedQuantity)> ProposeAsync(
        int needId,
        int resourceId,
        int quantity,
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 1 || quantity > FieldLimits.MaxQuantity)
        {
            return (ProposeMatchResult.InvalidQuantity, null, null);
        }

        var need = await _needRepository.GetByIdAsync(needId, cancellationToken);

        if (need is null)
        {
            return (ProposeMatchResult.NeedNotFound, null, null);
        }

        if (need.UserId != userId)
        {
            return (ProposeMatchResult.UserIsNotNeedOwner, null, null);
        }

        if (need.Status is not (NeedStatus.New or NeedStatus.UnderReview))
        {
            return (ProposeMatchResult.NeedNotOpen, null, null);
        }

        var resource = await _resourceRepository.GetByIdAsync(resourceId, cancellationToken);

        if (resource is null)
        {
            return (ProposeMatchResult.ResourceNotFound, null, null);
        }

        if (resource.Status != ResourceStatus.Available)
        {
            return (ProposeMatchResult.ResourceNotAvailable, null, null);
        }

        var alreadyMatched = await _matchRepository.ExistsAsync(
            needId,
            resourceId,
            cancellationToken);

        if (alreadyMatched)
        {
            return (ProposeMatchResult.MatchAlreadyExists, null, null);
        }

        if (!MatchRules.IsSuitable(need, resource, DateTime.UtcNow))
        {
            return (ProposeMatchResult.ResourceNotSuitable, null, null);
        }

        var allocatedNeedQuantity = await _matchRepository.GetAllocatedQuantityForNeedAsync(needId, cancellationToken);
        var allocatedResourceQuantity = await _matchRepository.GetAllocatedQuantityForResourceAsync(resourceId, cancellationToken);

        var remainingNeedQuantity = Math.Max(0, need.QuantityNeeded - allocatedNeedQuantity);
        var remainingResourceQuantity = Math.Max(0, resource.QuantityOffered - allocatedResourceQuantity);

        var maxAllowedQuantity = Math.Min(remainingNeedQuantity, remainingResourceQuantity);

        if (quantity > maxAllowedQuantity)
        {
            return (ProposeMatchResult.ExceedsRemaining, null, maxAllowedQuantity);
        }

        var match = new Match
        {
            NeedId = needId,
            ResourceId = resourceId,
            MatchedByUserId = userId,
            Quantity = quantity
        };

        await _matchRepository.AddAsync(match, cancellationToken);

        if (need.Status == NeedStatus.New)
        {
            need.Status = NeedStatus.UnderReview;
        }

        // Mark the Need for a version check, so two people cannot propose matches on the same need at the same time.
        _needRepository.MarkForConcurrencyCheck(need);

        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return (ProposeMatchResult.MatchAlreadyExists, null, null);
        }
        catch (ConcurrencyConflictException)
        {
            return (ProposeMatchResult.ConcurrentChange, null, null);
        }

        return (ProposeMatchResult.Success, match.Id, null);
    }

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

        if (match.Need.Status is not (NeedStatus.New or NeedStatus.UnderReview))
        {
            return MatchDecisionResult.NeedNotOpen;
        }

        if (match.Resource.Status != ResourceStatus.Available)
        {
            return MatchDecisionResult.ResourceNotAvailable;
        }

        if (!MatchRules.IsSuitable(match.Need, match.Resource, DateTime.UtcNow))
        {
            return MatchDecisionResult.ResourceNotSuitable;
        }

        var allocatedNeedQuantity = await _matchRepository.GetAllocatedQuantityForNeedAsync(match.NeedId, cancellationToken);
        var allocatedResourceQuantity = await _matchRepository.GetAllocatedQuantityForResourceAsync(match.ResourceId, cancellationToken);

        var remainingNeedQuantity = Math.Max(0, match.Need.QuantityNeeded - allocatedNeedQuantity);
        var remainingResourceQuantity = Math.Max(0, match.Resource.QuantityOffered - allocatedResourceQuantity);

        if (match.Quantity > remainingNeedQuantity || match.Quantity > remainingResourceQuantity)
        {
            return MatchDecisionResult.InsufficientQuantity;
        }

        match.Status = MatchStatus.Assigned;

        var matchesToCancel = new Dictionary<int, Match>();

        // Check whether the Need has been fully allocated.
        if (allocatedNeedQuantity + match.Quantity >= match.Need.QuantityNeeded)
        {
            // Mark the Need as assigned once its required quantity has been fulfilled.
            match.Need.Status = NeedStatus.Assigned;

            // Collect all other pending proposals for this Need.
            var pendingMatches = await _matchRepository.GetPendingMatchesByNeedIdAsync(match.NeedId, [match.Id], cancellationToken);

            foreach (var pendingMatch in pendingMatches)
            {
                matchesToCancel[pendingMatch.Id] = pendingMatch;
            }
        }

        // Check whether the Resource has been fully allocated.
        if (allocatedResourceQuantity + match.Quantity >= match.Resource.QuantityOffered)
        {
            // Mark the Resource as busy once its entire offered quantity has been allocated.
            match.Resource.Status = ResourceStatus.Busy;

            // Collect all other pending proposals for this Resource.
            var pendingMatches = await _matchRepository.GetPendingMatchesByResourceIdAsync(match.ResourceId, match.Id, cancellationToken);

            foreach (var pendingMatch in pendingMatches)
            {
                matchesToCancel[pendingMatch.Id] = pendingMatch;
            }
        }

        // Cancel all collected pending proposals.
        foreach (var pendingMatch in matchesToCancel.Values)
        {
            pendingMatch.Status = MatchStatus.Cancelled;
        }

        // Recalculate the status of affected Needs.
        foreach (var needId in matchesToCancel.Values.Select(m => m.NeedId).Distinct())
        {
            // Do not overwrite a Need that has already been fully allocated.
            if (needId == match.NeedId && match.Need.Status == NeedStatus.Assigned)
            {
                continue;
            }

            var affectedNeed = matchesToCancel.Values.First(m => m.NeedId == needId).Need;

            var allocatedQuantity = await _matchRepository.GetAllocatedQuantityForNeedAsync(needId, cancellationToken);

            var excludedMatchIds = matchesToCancel.Keys.ToArray();
            var pendingMatches = await _matchRepository.GetPendingMatchesByNeedIdAsync(needId, excludedMatchIds, cancellationToken);

            var hasRemainingPendingMatches = pendingMatches.Count > 0;

            affectedNeed.Status = allocatedQuantity > 0 || hasRemainingPendingMatches
                ? NeedStatus.UnderReview
                : NeedStatus.New;

            _needRepository.MarkForConcurrencyCheck(affectedNeed);
        }

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

        match.Status = MatchStatus.Rejected;

        var fulfilled = await _matchRepository.GetAllocatedQuantityForNeedAsync(match.Need.Id, cancellationToken);
        var waiting = await _matchRepository.CountPendingForNeedAsync(match.Need.Id, match.Id, cancellationToken);

        if (fulfilled > 0 || waiting > 0)
        {
            match.Need.Status = NeedStatus.UnderReview;
        }
        else
        {
            match.Need.Status = NeedStatus.New;
        }

        // Prevents concurrent operations from overwriting Need status changes.
        _needRepository.MarkForConcurrencyCheck(match.Need);

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