using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

public interface IAuditLogger
{
    void Record(
        AuditAction action,
        string entityType,
        int? entityId,
        string? details = null);

    void Record(
        AuditAction action,
        string entityType,
        int? entityId,
        int? actorUserId,
        string actorName,
        string? details = null);
}
