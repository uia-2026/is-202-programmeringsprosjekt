using Heimevernet.ViewModels.Match;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Possible outcomes of <see cref="IMatchService.ProposeAsync"/>.
/// </summary>
public enum ProposeMatchResult
{
    /// <summary>The match was created.</summary>
    Success,
    /// <summary>The quantity was below 1 or above the allowed maximum.</summary>
    InvalidQuantity,
    /// <summary>The need does not exist.</summary>
    NeedNotFound,
    /// <summary>The need is no longer New or UnderReview, so it cannot get new proposals.</summary>
    NeedNotOpen,
    /// <summary>The resource does not exist.</summary>
    ResourceNotFound,
    /// <summary>The resource is not in the Available status.</summary>
    ResourceNotAvailable,
    /// <summary>A match between this need and resource already exists.</summary>
    MatchAlreadyExists,
    /// <summary>The user does not own the need.</summary>
    UserIsNotNeedOwner,
    /// <summary>The resource does not fit the need (category or availability period).</summary>
    ResourceNotSuitable,
    /// <summary>The quantity is larger than what is left on the need or the resource.</summary>
    ExceedsRemaining,
    /// <summary>Someone else changed the need at the same time; the user should try again.</summary>
    ConcurrentChange
}

// NYTT I STEG 6
/// <summary>
/// Possible outcomes of <see cref="IMatchService.AcceptAsync"/> and <see cref="IMatchService.RejectAsync"/>.
/// </summary>
public enum MatchDecisionResult
{
    /// <summary>The decision was saved.</summary>
    Success,
    /// <summary>The match does not exist.</summary>
    MatchNotFound,
    /// <summary>The user does not own the resource in the match.</summary>
    UserIsNotResourceOwner,
    /// <summary>The match has already been decided.</summary>
    MatchNotUnderReview,
    /// <summary>There is not enough quantity left on the need or the resource.</summary>
    InsufficientQuantity,
    /// <summary>The need is no longer open for matches.</summary>
    NeedNotOpen,
    /// <summary>The resource no longer fits the need.</summary>
    ResourceNotSuitable,
    /// <summary>The resource is not in the Available status.</summary>
    ResourceNotAvailable,
    /// <summary>Someone else changed the data at the same time; the user should try again.</summary>
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
    /// <param name="needId">ID of the need to find resources for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The candidates sorted by distance. Empty if the need does not exist or is not open.</returns>
    Task<IReadOnlyList<MatchCandidateViewModel>> GetCandidateResourcesAsync(
            int needId,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true only if the Need exists and is owned by the given user.
    /// Used to gate access to the Propose page itself (defense in depth alongside ProposeAsync's own check).
    /// </summary>
    /// <param name="needId">ID of the need.</param>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><see langword="true"/> if the user owns the need and it is still open.</returns>
    Task<bool> CanProposeAsync(int needId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and creates a Match between the given Need and Resource.
    /// Only the Need's owner may propose a match for it.
    /// </summary>
    /// <param name="needId">ID of the need.</param>
    /// <param name="resourceId">ID of the resource to propose.</param>
    /// <param name="quantity">How many units to allocate.</param>
    /// <param name="userId">ID of the user proposing. Must own the need.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// The result code, the new match ID on success, and the highest allowed quantity
    /// when the result is <see cref="ProposeMatchResult.ExceedsRemaining"/>.
    /// </returns>
    Task<(ProposeMatchResult Result, int? MatchId, int? MaxAllowedQuantity)> ProposeAsync(int needId, int resourceId, int quantity, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the "My Matches" page model for the given user.
    /// Includes every match where the user owns the Need, the Resource, or both, regardless of role.
    /// </summary>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The page model with all of the user's matches.</returns>
    Task<MatchIndexViewModel> GetMyMatchesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts a match. Only the owner of the match's Resource may accept,
    /// and only while the match is UnderReview and there is enough quantity left.
    /// </summary>
    /// <param name="matchId">ID of the match to accept.</param>
    /// <param name="userId">ID of the user accepting. Must own the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="MatchDecisionResult"/> that tells if the match was accepted.</returns>
    Task<MatchDecisionResult> AcceptAsync(int matchId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects a match by removing it. Only the owner of the match's Resource may reject,
    /// and only while the match is UnderReview.
    /// </summary>
    /// <param name="matchId">ID of the match to reject.</param>
    /// <param name="userId">ID of the user rejecting. Must own the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="MatchDecisionResult"/> that tells if the match was rejected.</returns>
    Task<MatchDecisionResult> RejectAsync(int matchId, int userId, CancellationToken cancellationToken = default);
}