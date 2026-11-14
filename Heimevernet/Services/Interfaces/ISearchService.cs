using Heimevernet.DTOs.Search;
using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Free-text search across needs and resources.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches needs and resources by text. Resource providers only find their own resources;
    /// other roles find resources from everyone.
    /// </summary>
    /// <param name="query">The search text. Leading and trailing spaces are removed.</param>
    /// <param name="limit">Maximum number of hits per kind (needs and resources).</param>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="userRole">Role of the signed-in user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A <see cref="SearchResponseDto"/> with need hits followed by resource hits.</returns>
    Task<SearchResponseDto> SearchAsync(
        string query,
        int limit,
        int userId,
        UserRole userRole,
        CancellationToken cancellationToken);
}