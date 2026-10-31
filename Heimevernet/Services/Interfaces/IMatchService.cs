using Heimevernet.ViewModels.Match;

namespace Heimevernet.Services.Interfaces;

public enum ProposeMatchResult
{
    Success,
    InvalidQuantity,
    NeedNotFound,
    NeedNotOpen,
    ResourceNotFound,
    ResourceNotAvailable,
    MatchAlreadyExists,
    UserIsNotNeedOwner,
    ResourceNotSuitable,
    ExceedsRemaining,
    ConcurrentChange
}

// NYTT I STEG 6
public enum MatchDecisionResult
{
    Success,
    MatchNotFound,
    UserIsNotResourceOwner,
    MatchNotUnderReview,
    InsufficientQuantity,
    NeedNotOpen,
    ResourceNotSuitable,
    ResourceNotAvailable,
    ConcurrentChange
}

/// <summary>
/// Business logic for proposing a Resource to fulfill a Need (Match: Propose flow, #40).
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// Returns candidate Resources for a given Need, ranked by distance,
    /// filtered by category compatibility, availability status, and availability period.
    /// </summary>
    Task<IReadOnlyList<MatchCandidateViewModel>> GetCandidateResourcesAsync(
            int needId,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true only if the Need exists and is owned by the given user.
    /// Used to gate access to the Propose page itself (defense in depth alongside ProposeAsync's own check).
    /// </summary>
    Task<bool> CanProposeAsync(int needId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and creates a Match between the given Need and Resource.
    /// Only the Need's owner may propose a match for it.
    /// </summary>
    Task<(ProposeMatchResult Result, int? MatchId, int? MaxAllowedQuantity)> ProposeAsync(int needId, int resourceId, int quantity, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the "My Matches" page model for the given user.
    /// Resource Providers see matches on their resources; Public Actors see matches on their needs.
    /// </summary>
    Task<MatchIndexViewModel> GetMyMatchesAsync(int userId, bool isResourceProvider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a match. Only the owner of the match's Resource may accept,
    /// and only while the match is UnderReview and there is enough quantity left.
    /// </summary>
    Task<MatchDecisionResult> AcceptAsync(int matchId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a match by removing it. Only the owner of the match's Resource may reject,
    /// and only while the match is UnderReview.
    /// </summary>
    Task<MatchDecisionResult> RejectAsync(int matchId, int userId, CancellationToken cancellationToken = default);
}