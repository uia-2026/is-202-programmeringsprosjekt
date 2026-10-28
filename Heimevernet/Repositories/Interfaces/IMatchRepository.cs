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
    Task<int> GetAcceptedAllocatedQuantityForNeedAsync(int needId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums quantities from accepted matches for a specific resource.
    /// </summary>
    Task<int> GetAcceptedAllocatedQuantityForResourceAsync(int resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all matches for needs owned by the given user (Public Actor view).
    /// </summary>
    Task<IReadOnlyList<Match>> GetForNeedOwnerAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all matches for resources owned by the given user (Resource Provider view).
    /// </summary>
    Task<IReadOnlyList<Match>> GetForResourceOwnerAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a tracked match including its Need and Resource, or null.
    /// </summary>
    Task<Match?> GetByIdWithNeedAndResourceAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a match for deletion.
    /// </summary>
    void Remove(Match match);
}