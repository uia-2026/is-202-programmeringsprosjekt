namespace Heimevernet.Models;

/// <summary>
/// Historical record of a meaningful action performed in the application.
/// </summary>
public class AuditLog
{
    /// <summary>Primary key.</summary>
    public long Id { get; set; }
    /// <summary>When the action happened (UTC).</summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    /// <summary>ID of the user who did the action, or <see langword="null"/> for anonymous or unknown actors.</summary>
    public int? ActorUserId { get; set; }
    /// <summary>Username of the actor at the time, kept even if the user is later deleted. Max <see cref="FieldLimits.AuditActorName"/> characters.</summary>
    public string ActorName { get; set; } = string.Empty;
    /// <summary>What happened.</summary>
    public AuditAction Action { get; set; }
    /// <summary>Type name of the affected entity, e.g. "Need". Max <see cref="FieldLimits.AuditEntityType"/> characters.</summary>
    public string EntityType { get; set; } = string.Empty;
    /// <summary>ID of the affected entity, or <see langword="null"/> if there is none.</summary>
    public int? EntityId { get; set; }
    /// <summary>Optional extra details, e.g. changed fields. Max <see cref="FieldLimits.AuditDetails"/> characters.</summary>
    public string? Details { get; set; }
    /// <summary>Trace ID of the request, so the entry can be linked to the logs.</summary>
    public string? TraceId { get; set; }

    /// <summary>The user who did the action, if known.</summary>
    public User? ActorUser { get; set; }
}