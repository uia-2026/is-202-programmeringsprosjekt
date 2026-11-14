using Heimevernet.Repositories.Interfaces;

namespace Heimevernet.Infrastructure.Outbox;

/// <summary>
/// Takes due outbox messages one at a time: claims them, hands them to the handler for their type,
/// and records the result. Delivery is at-least-once: if the process dies after a handler succeeded
/// but before the row is marked Done, the message is handled again once its lease expires.
/// </summary>
public sealed class OutboxProcessor
{
    private readonly IOutboxRepository _repository;
    private readonly IReadOnlyDictionary<string, IOutboxHandler> _handlers;
    private readonly TimeProvider _time;
    private readonly ILogger<OutboxProcessor> _logger;

    /// <summary>
    /// Creates the processor.
    /// </summary>
    /// <param name="repository">Repository for outbox rows.</param>
    /// <param name="handlers">All registered handlers. There must be at most one per message type.</param>
    /// <param name="time">Clock, so tests can control the time.</param>
    /// <param name="logger">Logger.</param>
    /// <exception cref="ArgumentException">Thrown when two handlers have the same <see cref="IOutboxHandler.Type"/>.</exception>
    public OutboxProcessor(
        IOutboxRepository repository,
        IEnumerable<IOutboxHandler> handlers,
        TimeProvider time,
        ILogger<OutboxProcessor> logger)
    {
        _repository = repository;
        _handlers = handlers.ToDictionary(h => h.Type);   // two handlers for one type is a programming error and fails fast
        _time = time;
        _logger = logger;
    }

    private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Returns the IDs of messages that are due now.
    /// </summary>
    /// <param name="take">Maximum number of IDs.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The due message IDs.</returns>
    public Task<IReadOnlyList<long>> GetDueIdsAsync(int take, CancellationToken ct) =>
        _repository.GetDueIdsAsync(UtcNow, take, ct);

    /// <summary>
    /// Claims one message, runs its handler, and marks it Done, or schedules a retry if the handler throws.
    /// Does nothing if another worker already holds the message.
    /// </summary>
    /// <param name="id">ID of the message.</param>
    /// <param name="ct">Cancellation token. If cancelled during handling, the message stays claimed and is retried after the lease.</param>
    /// <returns>A task that completes when the message has been processed.</returns>
    public async Task ProcessOneAsync(long id, CancellationToken ct)
    {
        // Another instance may have taken it first: then there is nothing to do
        if (!await _repository.TryClaimAsync(id, UtcNow, OutboxPolicy.Lease, ct))
            return;

        // Read after the claim, so Attempts already includes this attempt
        var message = await _repository.GetAsync(id, ct);
        if (message is null)
            return;

        try
        {
            if (!_handlers.TryGetValue(message.Type, out var handler))
                throw new InvalidOperationException($"No handler for outbox type '{message.Type}'.");

            await handler.HandleAsync(message, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;   // shutting down: the row stays Processing and is picked up again after the lease
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Outbox message {MessageId} ({Type}) failed on attempt {Attempts}",
                id, message.Type, message.Attempts);

            // Only the message text is stored, never a stack trace
            await _repository.MarkFailedOrRetryAsync(id, message.Attempts, ex.Message, UtcNow, ct);
            return;
        }

        await _repository.MarkDoneAsync(id, UtcNow, ct);
    }
}