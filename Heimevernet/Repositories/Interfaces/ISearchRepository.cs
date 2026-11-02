using Heimevernet.DTOs.Search;
using Heimevernet.Models;
namespace Heimevernet.Repositories.Interfaces;

public interface ISearchRepository
{
    Task<IReadOnlyList<SearchResultDto>> SearchNeedsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchResultDto>> SearchResourcesAsync(
        string query,
        int limit,
        int userId,
        UserRole userRole,
        CancellationToken cancellationToken);
}