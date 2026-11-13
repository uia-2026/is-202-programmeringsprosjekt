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

    /// <summary>
    /// Handles one message.
    /// </summary>
    /// <param name="message">The claimed message. <see cref="OutboxMessage.Attempts"/> already counts this attempt.</param>
    /// <param name="ct">Cancellation token; cancelled when the app shuts down.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    Task HandleAsync(OutboxMessage message, CancellationToken ct);
}