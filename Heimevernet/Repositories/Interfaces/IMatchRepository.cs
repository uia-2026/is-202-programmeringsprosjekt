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
    Task AddAsync(Match match, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums quantities from accepted matches for a specific need.
    /// </summary>
    Task<int> GetAllocatedQuantityForNeedAsync(int needId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums quantities from accepted matches for a specific resource.
    /// </summary>
    Task<int> GetAllocatedQuantityForResourceAsync(int resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all matches where the given user owns the need, the resource, or both.
    /// </summary>
    Task<IReadOnlyList<Match>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked match including its Need and Resource, or null.
    /// </summary>
    Task<Match?> GetByIdWithNeedAndResourceAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all pending matches for the specified need.
    /// </summary>
    Task<List<Match>> GetPendingMatchesByNeedIdAsync(
            int needId,
            IReadOnlyCollection<int> excludedMatchIds,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all pending matches for the specified resource.
    /// </summary>
    Task<IReadOnlyList<Match>> GetPendingMatchesByResourceIdAsync(int resourceId, int excludeMatchId, CancellationToken ct = default);

    /// <summary>
    /// Returns true if the user is part of any Assigned match: as need owner, resource owner, or the one who created it.
    /// </summary>
    Task<bool> HasAssignedMatchForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<int> CountPendingForNeedAsync(int needId, int excludedMatchId, CancellationToken ct = default);
}