using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

/// <summary>
/// Repository implementation for Matches.
/// Provides data access operations used by MatchService.
/// </summary>
public class MatchRepository : IMatchRepository
{
    private readonly AppDbContext _context;

    public MatchRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Checks whether a Match already exists between the given Need and Resource.
    /// </summary>
    public async Task<bool> ExistsAsync(int needId, int resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .AnyAsync(m => m.NeedId == needId && m.ResourceId == resourceId, cancellationToken);
    }

    /// <summary>
    /// Adds a Match entity to the context for insertion.
    /// </summary>
    public async Task AddAsync(Match match, CancellationToken cancellationToken = default)
    {
        await _context.Matches.AddAsync(match, cancellationToken);
    }
}