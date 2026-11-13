using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuditRepository"/>.
/// </summary>
/// <param name="context">The database context.</param>
public sealed class AuditRepository(AppDbContext context) : IAuditRepository
{
    /// <inheritdoc />
    public void Add(AuditLog auditLog) => context.AuditLogs.Add(auditLog);

    /// <inheritdoc />
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

    /// <inheritdoc />
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