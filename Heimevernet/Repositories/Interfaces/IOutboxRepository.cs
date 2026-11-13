using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access for <see cref="OutboxMessage"/> rows used by the outbox worker.
/// The update methods run directly in the database and do not need a commit.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>Adds a message to the context. Saved by the next unit of work commit.</summary>
    /// <param name="message">The message to add.</param>
    void Add(OutboxMessage message);

    /// <summary>
    /// Reads a message without tracking it.
    /// </summary>
    /// <param name="id">ID of the message.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The message, or <see langword="null"/> if it does not exist.</returns>
    Task<OutboxMessage?> GetAsync(long id, CancellationToken ct = default);

    /// <summary>Ids of messages that are due: pending ones whose time has come, and abandoned ones whose lease expired.</summary>
    /// <param name="now">The current UTC time.</param>
    /// <param name="take">Maximum number of IDs to return.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The IDs of due messages, oldest first.</returns>
    Task<IReadOnlyList<long>> GetDueIdsAsync(DateTime now, int take, CancellationToken ct = default);

    /// <summary>
    /// Tries to take a due message. Returns true only for the one worker whose update changed the row.
    /// Increments Attempts as part of the claim.
    /// </summary>
    /// <param name="id">ID of the message to claim.</param>
    /// <param name="now">The current UTC time.</param>
    /// <param name="lease">How long the claim lasts before another worker may take over.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns><see langword="true"/> if this worker got the message.</returns>
    Task<bool> TryClaimAsync(long id, DateTime now, TimeSpan lease, CancellationToken ct = default);

    /// <summary>
    /// Marks a message as Done and clears its lock and last error.
    /// </summary>
    /// <param name="id">ID of the message.</param>
    /// <param name="now">The current UTC time, saved as the processed time.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the row has been updated.</returns>
    Task MarkDoneAsync(long id, DateTime now, CancellationToken ct = default);

    /// <summary>Schedules a retry, or marks the message Failed when all attempts are used up.</summary>
    /// <param name="id">ID of the message.</param>
    /// <param name="attempts">Number of attempts used so far.</param>
    /// <param name="error">The error text. It is cut to the column length if too long.</param>
    /// <param name="now">The current UTC time.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the row has been updated.</returns>
    Task MarkFailedOrRetryAsync(long id, int attempts, string error, DateTime now, CancellationToken ct = default);

    /// <summary>
    /// Number of finished messages of a type for a recipient since a time. Used for a daily cap.
    /// Messages that were skipped (stale or opted out) are also marked Done, so this slightly overcounts.
    /// </summary>
    /// <param name="recipientUserId">ID of the user who receives the messages.</param>
    /// <param name="type">Message type, see <see cref="Heimevernet.Infrastructure.Outbox.OutboxTypes"/>.</param>
    /// <param name="since">Start of the time window (UTC).</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The number of Done messages in the window.</returns>
    Task<int> CountProcessedSinceAsync(int recipientUserId, string type, DateTime since, CancellationToken ct = default);
}