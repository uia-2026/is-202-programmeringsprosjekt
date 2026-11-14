using Heimevernet.Models;

namespace Heimevernet.DTOs.Audit;

/// <summary>
/// One audit entry as returned by the audit API (<c>GET /api/audit/...</c>).
/// </summary>
/// <param name="Id">ID of the entry.</param>
/// <param name="OccurredAt">When the action happened (UTC).</param>
/// <param name="ActorUserId">ID of the user who did the action, or <see langword="null"/> if unknown.</param>
/// <param name="ActorName">Name of the actor at the time.</param>
/// <param name="Action">What happened.</param>
/// <param name="EntityType">Type name of the affected entity.</param>
/// <param name="EntityId">ID of the affected entity, if any.</param>
/// <param name="Details">Optional extra details.</param>
/// <param name="TraceId">Trace ID of the request, for finding it in the logs.</param>
public sealed record AuditLogDto(
    long Id,
    DateTime OccurredAt,
    int? ActorUserId,
    string ActorName,
    AuditAction Action,
    string EntityType,
    int? EntityId,
    string? Details,
    string? TraceId);