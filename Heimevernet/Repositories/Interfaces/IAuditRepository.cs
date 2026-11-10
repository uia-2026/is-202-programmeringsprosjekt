using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IAuditRepository
{
    void Add(AuditLog auditLog);

    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every audit entry performed by the given user, newest first.
    /// </summary>
    Task<IReadOnlyList<AuditLog>> GetByActorAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);
}
