using Heimevernet.Models;

namespace Heimevernet.Infrastructure.Outbox;

/// <summary>
/// Handles one kind of outbox message. Throw to have the message retried later. Return normally when the
/// message is done, and also when it no longer needs to be sent (a stale need, a removed user, an opt-out).
/// Handlers may run twice for the same message, so they must be safe to repeat.
/// </summary>
public interface IOutboxHandler
{
    /// <summary>The message type this handler is responsible for, one of <see cref="OutboxTypes"/>.</summary>
    string Type { get; }

    Task HandleAsync(OutboxMessage message, CancellationToken ct);
}