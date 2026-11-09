using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

public sealed class AuditRepository(AppDbContext context) : IAuditRepository
{
    public void Add(AuditLog auditLog) => context.AuditLogs.Add(auditLog);

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default) =>
        await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderBy(a => a.OccurredAt)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> GetByActorAsync(
        int actorUserId,
        CancellationToken cancellationToken = default) =>
        await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.ActorUserId == actorUserId)
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);
}
