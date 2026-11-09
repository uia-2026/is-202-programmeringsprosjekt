using Heimevernet.DTOs.Audit;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Controllers;

[Authorize(Roles = nameof(UserRole.ResourceProvider))]
[Route("api/audit")]
public sealed class AuditController(IAuditRepository auditRepository) : ControllerBase
{
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
