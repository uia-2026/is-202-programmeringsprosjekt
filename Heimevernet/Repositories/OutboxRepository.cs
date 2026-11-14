using Heimevernet.Data;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOutboxRepository"/>. Claims and status changes use
/// <c>ExecuteUpdateAsync</c> so they are single atomic SQL statements.
/// </summary>
public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="context">The database context.</param>
    public OutboxRepository(AppDbContext context) => _context = context;

    /// <inheritdoc />
    public void Add(OutboxMessage message) => _context.OutboxMessages.Add(message);

    /// <inheritdoc />
    public Task<OutboxMessage?> GetAsync(long id, CancellationToken ct = default) =>
        _context.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> GetDueIdsAsync(DateTime now, int take, CancellationToken ct = default)
    {
        // Two simple queries instead of one with OR, so each can use the (Status, NextAttemptAt) index
        var pending = await _context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Status == OutboxStatus.Pending && m.NextAttemptAt <= now)
            .OrderBy(m => m.NextAttemptAt)
            .Select(m => m.Id)
            .Take(take)
            .ToListAsync(ct);

        var abandoned = await _context.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Status == OutboxStatus.Processing && m.LockedUntil < now)
            .OrderBy(m => m.LockedUntil)
            .Select(m => m.Id)
            .Take(take)
            .ToListAsync(ct);

        return pending.Concat(abandoned).Distinct().Take(take).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> TryClaimAsync(long id, DateTime now, TimeSpan lease, CancellationToken ct = default)
    {
        var lockedUntil = now.Add(lease);

        // One atomic statement: it only changes the row if it is still due and nobody else holds it
        var rows = await _context.OutboxMessages
            .Where(m => m.Id == id
                     && ((m.Status == OutboxStatus.Pending && m.NextAttemptAt <= now)
                      || (m.Status == OutboxStatus.Processing && m.LockedUntil < now)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxStatus.Processing)
                .SetProperty(m => m.LockedUntil, (DateTime?)lockedUntil)
                .SetProperty(m => m.Attempts, m => m.Attempts + 1), ct);

        return rows == 1;
    }

    /// <inheritdoc />
    public Task MarkDoneAsync(long id, DateTime now, CancellationToken ct = default) =>
        _context.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, OutboxStatus.Done)
                .SetProperty(m => m.ProcessedAt, (DateTime?)now)
                .SetProperty(m => m.LockedUntil, (DateTime?)null)
                .SetProperty(m => m.LastError, (string?)null), ct);

    /// <inheritdoc />
    public Task MarkFailedOrRetryAsync(long id, int attempts, string error, DateTime now, CancellationToken ct = default)
    {
        var exhausted = OutboxPolicy.IsExhausted(attempts);
        var status = exhausted ? OutboxStatus.Failed : OutboxStatus.Pending;
        var nextAttempt = now.Add(OutboxPolicy.DelayAfter(attempts));
        DateTime? processedAt = exhausted ? now : null;
        var message = error.Length <= FieldLimits.OutboxError ? error : error[..FieldLimits.OutboxError];

        return _context.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, status)
                .SetProperty(m => m.NextAttemptAt, nextAttempt)
                .SetProperty(m => m.LockedUntil, (DateTime?)null)
                .SetProperty(m => m.LastError, (string?)message)
                .SetProperty(m => m.ProcessedAt, processedAt), ct);
    }

    /// <inheritdoc />
    public Task<int> CountProcessedSinceAsync(int recipientUserId, string type, DateTime since, CancellationToken ct = default) =>
        _context.OutboxMessages
            .AsNoTracking()
            .CountAsync(m => m.RecipientUserId == recipientUserId
                          && m.Type == type
                          && m.Status == OutboxStatus.Done
                          && m.ProcessedAt >= since, ct);
}