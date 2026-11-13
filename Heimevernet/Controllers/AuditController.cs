using Heimevernet.DTOs.Audit;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

/// <summary>
/// JSON API for reading the audit history of an entity.
/// </summary>
/// <remarks>Base route <c>/api/audit</c>. Only for users in the <see cref="UserRole.ResourceProvider"/> role.</remarks>
/// <param name="auditRepository">Repository used to read audit entries.</param>
[Authorize(Roles = nameof(UserRole.ResourceProvider))]
[Route("api/audit")]
public sealed class AuditController(IAuditRepository auditRepository) : ControllerBase
{
    /// <summary>
    /// Returns the audit history for one entity, oldest first.
    /// </summary>
    /// <remarks><c>GET /api/audit/{entityType}/{entityId}</c>. Requires the ResourceProvider role.</remarks>
    /// <param name="entityType">Entity type name, e.g. "Need" or "Resource".</param>
    /// <param name="entityId">ID of the entity.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>200 OK with a list of <see cref="AuditLogDto"/> (empty if there is no history).</returns>
    [HttpGet("{entityType}/{entityId:int}")]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetEntityHistory(
        string entityType,
        int entityId,
        CancellationToken cancellationToken)
    {
        var entries = await auditRepository.GetByEntityAsync(entityType, entityId, cancellationToken);

        return Ok(entries.Select(a => new AuditLogDto(
            a.Id,
            a.OccurredAt,
            a.ActorUserId,
            a.ActorName,
            a.Action,
            a.EntityType,
            a.EntityId,
            a.Details,
            a.TraceId)));
    }
}