using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access for the <see cref="AuditLog"/> table.
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Adds an audit entry to the context. It is saved on the next unit of work commit.
    /// </summary>
    /// <param name="auditLog">The entry to add.</param>
    void Add(AuditLog auditLog);

    /// <summary>
    /// Returns the history of one entity, oldest first.
    /// </summary>
    /// <param name="entityType">Entity type name, e.g. "Need".</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The audit entries for the entity.</returns>
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        int entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every audit entry performed by the given user, newest first.
    /// </summary>
    /// <param name="actorUserId">ID of the user who did the actions.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The audit entries for the user.</returns>
    Task<IReadOnlyList<AuditLog>> GetByActorAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);
}