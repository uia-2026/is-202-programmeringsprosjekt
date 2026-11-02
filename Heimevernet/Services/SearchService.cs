using Heimevernet.DTOs.Search;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;

public sealed class SearchService : ISearchService
{
    private readonly ISearchRepository _searchRepository;

    public SearchService(ISearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }

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