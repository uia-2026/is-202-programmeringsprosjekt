using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IOutboxRepository
{
    /// <summary>Adds a message to the context. Saved by the next unit of work commit.</summary>
    void Add(OutboxMessage message);

    Task<OutboxMessage?> GetAsync(long id, CancellationToken ct = default);

    /// <summary>Ids of messages that are due: pending ones whose time has come, and abandoned ones whose lease expired.</summary>
    Task<IReadOnlyList<long>> GetDueIdsAsync(DateTime now, int take, CancellationToken ct = default);

    /// <summary>
    /// Tries to take a due message. Returns true only for the one worker whose update changed the row.
    /// Increments Attempts as part of the claim.
    /// </summary>
    Task<bool> TryClaimAsync(long id, DateTime now, TimeSpan lease, CancellationToken ct = default);

    Task MarkDoneAsync(long id, DateTime now, CancellationToken ct = default);

    /// <summary>Schedules a retry, or marks the message Failed when all attempts are used up.</summary>
    Task MarkFailedOrRetryAsync(long id, int attempts, string error, DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Number of finished messages of a type for a recipient since a time. Used for a daily cap.
    /// Messages that were skipped (stale or opted out) are also marked Done, so this slightly overcounts.
    /// </summary>
    Task<int> CountProcessedSinceAsync(int recipientUserId, string type, DateTime since, CancellationToken ct = default);
}