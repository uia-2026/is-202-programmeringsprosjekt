using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Adds entries to the audit log. Entries are only queued on the current unit of work;
/// they are saved when <see cref="Heimevernet.Repositories.Interfaces.IUnitOfWork.CommitAsync"/> is called.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Records an audit entry for the currently signed-in user (see <see cref="ICurrentUser"/>).
    /// </summary>
    /// <param name="action">What happened.</param>
    /// <param name="entityType">Name of the affected entity type, e.g. "Need" or "Resource".</param>
    /// <param name="entityId">ID of the affected entity, or <see langword="null"/> if there is none.</param>
    /// <param name="details">Optional free-text details, e.g. which fields were changed.</param>
    void Record(
        AuditAction action,
        string entityType,
        int? entityId,
        string? details = null);

    /// <summary>
    /// Records an audit entry for an explicitly given actor. Used when there is no signed-in user yet,
    /// for example during login.
    /// </summary>
    /// <param name="action">What happened.</param>
    /// <param name="entityType">Name of the affected entity type, e.g. "User".</param>
    /// <param name="entityId">ID of the affected entity, or <see langword="null"/> if there is none.</param>
    /// <param name="actorUserId">ID of the user who did the action, or <see langword="null"/> if unknown.</param>
    /// <param name="actorName">Display name of the actor, e.g. the username or "Unknown".</param>
    /// <param name="details">Optional free-text details.</param>
    void Record(
        AuditAction action,
        string entityType,
        int? entityId,
        int? actorUserId,
        string actorName,
        string? details = null);
}