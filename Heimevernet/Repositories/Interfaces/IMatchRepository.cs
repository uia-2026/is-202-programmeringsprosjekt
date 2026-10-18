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
}