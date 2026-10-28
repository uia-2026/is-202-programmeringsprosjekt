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

    public async Task<int> GetAcceptedAllocatedQuantityForNeedAsync(int needId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .Where(m => m.NeedId == needId && m.Status == MatchStatus.Assigned)
            .SumAsync(m => (int?)m.Quantity, cancellationToken) ?? 0;
    }

    public async Task<int> GetAcceptedAllocatedQuantityForResourceAsync(int resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .Where(m => m.ResourceId == resourceId && m.Status == MatchStatus.Assigned)
            .SumAsync(m => (int?)m.Quantity, cancellationToken) ?? 0;
    }

    // ---------- NYTT I STEG 1 ----------

    /// <summary>
    /// Returns all matches for needs owned by the given user (Public Actor view).
    /// </summary>
    public async Task<IReadOnlyList<Match>> GetForNeedOwnerAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .AsNoTracking()
            .Include(m => m.Need)
            .Include(m => m.Resource)
            .Where(m => m.Need.UserId == userId)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns all matches for resources owned by the given user (Resource Provider view).
    /// </summary>
    public async Task<IReadOnlyList<Match>> GetForResourceOwnerAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .AsNoTracking()
            .Include(m => m.Need)
            .Include(m => m.Resource)
            .Where(m => m.Resource.UserId == userId)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a tracked match including its Need and Resource, or null.
    /// </summary>
    public async Task<Match?> GetByIdWithNeedAndResourceAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .Include(m => m.Need)
            .Include(m => m.Resource)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <summary>
    /// Marks a match for deletion.
    /// </summary>
    public void Remove(Match match)
    {
        _context.Matches.Remove(match);
    }
}