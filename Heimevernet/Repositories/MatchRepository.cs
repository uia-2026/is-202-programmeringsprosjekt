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

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="context">The database context.</param>
    public MatchRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Checks whether a Match already exists between the given Need and Resource.
    /// </summary>
    /// <param name="needId">ID of the need.</param>
    /// <param name="resourceId">ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns><see langword="true"/> if a match already exists.</returns>
    public async Task<bool> ExistsAsync(int needId, int resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .AnyAsync(m => m.NeedId == needId && m.ResourceId == resourceId, cancellationToken);
    }

    /// <summary>
    /// Adds a Match entity to the context for insertion.
    /// </summary>
    /// <param name="match">The match to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task AddAsync(Match match, CancellationToken cancellationToken = default)
    {
        await _context.Matches.AddAsync(match, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetAllocatedQuantityForNeedAsync(int needId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .Where(m => m.NeedId == needId && (m.Status == MatchStatus.Assigned || m.Status == MatchStatus.Resolved))
            .SumAsync(m => (int?)m.Quantity, cancellationToken) ?? 0;
    }

    /// <inheritdoc />
    public async Task<int> GetAllocatedQuantityForResourceAsync(int resourceId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches.Where(m => m.ResourceId == resourceId && (m.Status == MatchStatus.Assigned || m.Status == MatchStatus.Resolved))
            .SumAsync(m => (int?)m.Quantity, cancellationToken) ?? 0;
    }

    /// <summary>
    /// Returns all matches where the given user owns the need, the resource, or both.
    /// A single query with OR returns each match once, even when the user owns both sides.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The matches, newest first.</returns>
    public async Task<IReadOnlyList<Match>> GetForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .AsNoTracking()
            .Include(m => m.Need)
            .Include(m => m.Resource)
            .Where(m => m.Need.UserId == userId || m.Resource.UserId == userId)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Returns a tracked match including its Need and Resource, or null.
    /// </summary>
    /// <param name="id">ID of the match.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The tracked match, or <see langword="null"/>.</returns>
    public async Task<Match?> GetByIdWithNeedAndResourceAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .Include(m => m.Need)
            .Include(m => m.Resource)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetPendingMatchesByNeedIdAsync(
        int needId,
        IReadOnlyCollection<int> excludedMatchIds,
        CancellationToken cancellationToken = default)
    {
        return await _context.Matches
            .Where(m => m.NeedId == needId &&
                        m.Status == MatchStatus.UnderReview &&
                        !excludedMatchIds.Contains(m.Id))
            .Include(m => m.Need)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Match>> GetPendingMatchesByResourceIdAsync(int resourceId, int excludeMatchId, CancellationToken ct = default) =>
       await _context.Matches
           .Include(m => m.Need)
           .Where(m => m.ResourceId == resourceId && m.Id != excludeMatchId && m.Status == MatchStatus.UnderReview)
           .ToListAsync(ct);

    /// <inheritdoc />
    public async Task<int> CountPendingForNeedAsync(
        int needId,
        int excludedMatchId,
        CancellationToken ct = default)
    {
        return await _context.Matches
            .Where(m => m.NeedId == needId && m.Id != excludedMatchId && m.Status == MatchStatus.UnderReview)
            .CountAsync(ct);
    }
}