using Heimevernet.DTOs.Search;
using Heimevernet.Models;
namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Database queries for the free-text search.
/// </summary>
public interface ISearchRepository
{
    /// <summary>
    /// Finds needs whose title contains the search text, highest priority first.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="limit">Maximum number of hits.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The hits as <see cref="SearchResultDto"/> items.</returns>
    Task<IReadOnlyList<SearchResultDto>> SearchNeedsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Finds resources whose title contains the search text, sorted by title.
    /// Resource providers only get their own resources.
    /// </summary>
    /// <param name="query">The search text.</param>
    /// <param name="limit">Maximum number of hits.</param>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="userRole">Role of the signed-in user.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The hits as <see cref="SearchResultDto"/> items.</returns>
    Task<IReadOnlyList<SearchResultDto>> SearchResourcesAsync(
        string query,
        int limit,
        int userId,
        UserRole userRole,
        CancellationToken cancellationToken);
}