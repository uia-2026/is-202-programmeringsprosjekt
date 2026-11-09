using System.Diagnostics;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;

namespace Heimevernet.Services;

public sealed class AuditLogger(
    IAuditRepository repository,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IAuditLogger
{
    public void Record(
        AuditAction action,
        string entityType,
        int? entityId,
        string? details = null)
    {
        Record(action, entityType, entityId, currentUser.UserId, currentUser.ActorName, details);
    }

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
