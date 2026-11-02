using Heimevernet.DTOs.Search;
using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

public interface ISearchService
{
    Task<SearchResponseDto> SearchAsync(
        string query,
        int limit,
        int userId,
        UserRole userRole,
        CancellationToken cancellationToken);
}