using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access contract for Match entities.
/// Implementations provide methods to query and persist Match instances.
/// </summary>
public interface IMatchRepository
{
    /// <summary>
    /// Checks whether a Match already exists between the given Need and Resource.
    /// </summary>
    /// <param name="needId">The need id.</param>
    /// <param name="resourceId">The resource id.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>True if a match already exists, otherwise false.</returns>
    Task<bool> ExistsAsync(int needId, int resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a Match entity to the context for insertion.
    /// </summary>
    /// <param name="match">The match entity to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the match has been added to the context.</returns>
    Task AddAsync(Match match, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums quantities from accepted matches for a specific need.
    /// </summary>
    /// <param name="needId">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The total quantity of Assigned and Resolved matches, or 0.</returns>
    Task<int> GetAllocatedQuantityForNeedAsync(int needId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums quantities from accepted matches for a specific resource.
    /// </summary>
    /// <param name="resourceId">ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The total quantity of Assigned and Resolved matches, or 0.</returns>
    Task<int> GetAllocatedQuantityForResourceAsync(int resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all matches where the given user owns the need, the resource, or both.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The matches, newest first, with need and resource loaded (not tracked).</returns>
    Task<IReadOnlyList<Match>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked match including its Need and Resource, or null.
    /// </summary>
    /// <param name="id">ID of the match.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The tracked match, or <see langword="null"/> if it does not exist.</returns>
    Task<Match?> GetByIdWithNeedAndResourceAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all pending matches for the specified need.
    /// </summary>
    /// <param name="needId">ID of the need.</param>
    /// <param name="excludedMatchIds">IDs of matches to leave out, e.g. the one being accepted.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The tracked UnderReview matches, with their need loaded.</returns>
    Task<List<Match>> GetPendingMatchesByNeedIdAsync(
            int needId,
            IReadOnlyCollection<int> excludedMatchIds,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all pending matches for the specified resource.
    /// </summary>
    /// <param name="resourceId">ID of the resource.</param>
    /// <param name="excludeMatchId">ID of a match to leave out, e.g. the one being accepted.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The tracked UnderReview matches, with their need loaded.</returns>
    Task<IReadOnlyList<Match>> GetPendingMatchesByResourceIdAsync(int resourceId, int excludeMatchId, CancellationToken ct = default);

    /// <summary>
    /// Counts the UnderReview matches for a need, not counting one given match.
    /// </summary>
    /// <param name="needId">ID of the need.</param>
    /// <param name="excludedMatchId">ID of a match to leave out of the count.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The number of other pending matches.</returns>
    Task<int> CountPendingForNeedAsync(int needId, int excludedMatchId, CancellationToken ct = default);
}