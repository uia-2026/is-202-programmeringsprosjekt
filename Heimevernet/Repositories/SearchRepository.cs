using Heimevernet.Data;
using Heimevernet.DTOs.Search;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of <see cref="ISearchRepository"/> using simple title <c>Contains</c> matching.
/// </summary>
public sealed class SearchRepository : ISearchRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SearchRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResultDto>> SearchNeedsAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _context.Needs
            .AsNoTracking()
            .Where(n => n.Title.Contains(query))
            .OrderByDescending(n => n.Priority)
            .ThenBy(n => n.Title)
            .Take(limit)
            .Select(n => new SearchResultDto(
                Type: SearchResultType.Need,
                Id: n.Id,
                Title: n.Title,
                Subtitle: $"{n.Priority} · {n.Region} · {n.Status}",
                Latitude: n.Latitude,
                Longitude: n.Longitude,
                Url: $"/Need/Details/{n.Id}"
            ))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResultDto>> SearchResourcesAsync(
        string query,
        int limit,
        int userId,
        UserRole userRole,
        CancellationToken cancellationToken)
    {
        var resources = _context.Resources
            .AsNoTracking()
            .Where(r => r.Title.Contains(query));

        if (userRole == UserRole.ResourceProvider)
        {
            resources = resources.Where(r => r.UserId == userId);
        }

        return await resources
            .OrderBy(r => r.Title)
            .Take(limit)
            .Select(r => new SearchResultDto(
                Type: SearchResultType.Resource,
                Id: r.Id,
                Title: r.Title,
                Subtitle: $"{r.Region} · {r.Status}",
                Latitude: r.Latitude,
                Longitude: r.Longitude,
                Url: $"/Resource/Details/{r.Id}"
            ))
            .ToListAsync(cancellationToken);
    }
}