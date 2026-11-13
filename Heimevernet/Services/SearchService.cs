using Heimevernet.DTOs.Search;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;

/// <summary>
/// Default <see cref="ISearchService"/> that runs separate need and resource searches and combines the hits.
/// </summary>
public sealed class SearchService : ISearchService
{
    private readonly ISearchRepository _searchRepository;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="searchRepository">Repository that runs the search queries.</param>
    public SearchService(ISearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }

    /// <inheritdoc />
    public async Task<SearchResponseDto> SearchAsync(
        string query,
        int limit,
        int userId,
        UserRole userRole,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();

        var needs = await _searchRepository.SearchNeedsAsync(
            normalizedQuery,
            limit,
            cancellationToken);

        var resources = await _searchRepository.SearchResourcesAsync(
            normalizedQuery,
            limit,
            userId,
            userRole,
            cancellationToken);

        var results = needs
            .Concat(resources)
            .ToList();

        return new SearchResponseDto(results);
    }
}