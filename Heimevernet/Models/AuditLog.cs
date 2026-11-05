namespace Heimevernet.Models;

/// <summary>
/// Historical record of a meaningful action performed in the application.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? TraceId { get; set; }

    public User? ActorUser { get; set; }
}
