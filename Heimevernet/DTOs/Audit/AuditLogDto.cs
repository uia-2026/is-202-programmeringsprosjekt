using Heimevernet.Models;

namespace Heimevernet.DTOs.Audit;

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
