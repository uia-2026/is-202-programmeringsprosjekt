namespace Heimevernet.Models;

/// <summary>
/// A message waiting to be delivered, written in the same transaction as the change it describes
/// (transactional outbox). A background worker sends it later with retries.
/// </summary>
public class OutboxMessage
{
    /// <summary>Primary key.</summary>
    public long Id { get; set; }

    /// <summary>What kind of message this is, so the worker knows how to handle it.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Unique per event and recipient, so the same event cannot be queued twice.</summary>
    public string DedupKey { get; set; } = string.Empty;

    /// <summary>The recipient, if any. The email address is looked up when the message is sent.</summary>
    public int? RecipientUserId { get; set; }

    /// <summary>Small JSON with ids only, never contact details.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Delivery state of the message.</summary>
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    /// <summary>How many times a worker has tried to send the message.</summary>
    public int Attempts { get; set; }

    /// <summary>Earliest time (UTC) the message may be tried again.</summary>
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

    /// <summary>While Processing: the claim expires at this time and another worker may take over.</summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>When the message ended as Done or Failed (UTC).</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Error text from the last failed attempt, if any.</summary>
    public string? LastError { get; set; }

    /// <summary>When the message was queued (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}