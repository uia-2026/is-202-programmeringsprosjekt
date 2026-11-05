using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IAuditRepository
{
    void Add(AuditLog auditLog);

    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default);
}
