using System.Diagnostics;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;

namespace Heimevernet.Services;

/// <summary>
/// Default <see cref="IAuditLogger"/> that adds <see cref="AuditLog"/> rows to the repository,
/// with the current trace ID so entries can be linked to logs.
/// </summary>
/// <param name="repository">Repository the entries are added to.</param>
/// <param name="currentUser">Gives the current actor for the short <c>Record</c> overload.</param>
/// <param name="httpContextAccessor">Used to read the request trace ID when no activity is running.</param>
public sealed class AuditLogger(
    IAuditRepository repository,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IAuditLogger
{
    /// <inheritdoc />
    public void Record(
        AuditAction action,
        string entityType,
        int? entityId,
        string? details = null)
    {
        Record(action, entityType, entityId, currentUser.UserId, currentUser.ActorName, details);
    }

    /// <inheritdoc />
    public void Record(
        AuditAction action,
        string entityType,
        int? entityId,
        int? actorUserId,
        string actorName,
        string? details = null)
    {
        repository.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ActorUserId = actorUserId,
            ActorName = actorName,
            Details = details,
            TraceId = Activity.Current?.Id ?? httpContextAccessor.HttpContext?.TraceIdentifier
        });
    }
}