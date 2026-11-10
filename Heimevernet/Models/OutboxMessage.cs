namespace Heimevernet.Models;

/// <summary>
/// A message waiting to be delivered, written in the same transaction as the change it describes
/// (transactional outbox). A background worker sends it later with retries.
/// </summary>
public class OutboxMessage
{
    public long Id { get; set; }

    /// <summary>What kind of message this is, so the worker knows how to handle it.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Unique per event and recipient, so the same event cannot be queued twice.</summary>
    public string DedupKey { get; set; } = string.Empty;

    /// <summary>The recipient, if any. The email address is looked up when the message is sent.</summary>
    public int? RecipientUserId { get; set; }

    /// <summary>Small JSON with ids only, never contact details.</summary>
    public string Payload { get; set; } = string.Empty;

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

    public int Attempts { get; set; }

    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

    /// <summary>While Processing: the claim expires at this time and another worker may take over.</summary>
    public DateTime? LockedUntil { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}