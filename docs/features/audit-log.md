# Audit logging

## Purpose

Heimevernet records important authentication events and domain changes in an append-only audit history. The history answers _who did what, to which record, and when_, for example:

- Who resolved this need, and when?
- Which matches were accepted for this resource?
- Have there been many failed logins for this account?

Audit entries never contain credentials or other secrets (see [What must never be logged](#what-must-never-be-logged)).

## Data model

```csharp
// Models/AuditLog.cs
public class AuditLog
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? TraceId { get; set; }

    public User? ActorUser { get; set; }
}
```

| Column        | Meaning                                                                                        |
| ------------- | ---------------------------------------------------------------------------------------------- |
| `Id`          | Database-generated identifier. `long`, because the log grows faster than any other table.      |
| `OccurredAt`  | UTC timestamp.                                                                                  |
| `ActorUserId` | The user who performed the action. `NULL` for unknown accounts or deleted users.                |
| `ActorName`   | Snapshot of the username, so the entry still shows who acted after a rename or deletion.       |
| `Action`      | An `AuditAction` value, stored as text (`NeedResolved`, not `6`).                              |
| `EntityType`  | The affected entity: `Need`, `Resource`, `Match`, `Attachment` or `User`.                      |
| `EntityId`    | The affected row. `NULL` when there is none, for example a failed login for an unknown email. |
| `Details`     | Short, non-sensitive context, such as which fields changed. Max 500 characters.                |
| `TraceId`     | `Activity.Current?.Id` or the HTTP trace identifier, to correlate with application logs.       |

### EF Core configuration

```csharp
// Data/AppDbContext.cs
modelBuilder.Entity<AuditLog>(b =>
{
    b.Property(a => a.Action).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
    // ...max lengths for ActorName, EntityType, Details and TraceId...
    b.HasIndex(a => new { a.EntityType, a.EntityId });
    b.HasIndex(a => a.OccurredAt);
    b.HasOne(a => a.ActorUser)
        .WithMany(u => u.AuditLogs)
        .HasForeignKey(a => a.ActorUserId)
        .OnDelete(DeleteBehavior.SetNull);
});
```

- **`(EntityType, EntityId)`** serves the history of one record, which is what the audit API asks for.
- **`OccurredAt`** serves chronological queries and future retention clean-up.
- **`SetNull`** means deleting a user keeps their audit entries. `ActorUserId` becomes `NULL`, and `ActorName` still shows who it was.

## Recorded actions

```csharp
// Models/Enums.cs
public enum AuditAction
{
    LoginSucceeded, LoginFailed, TwoFactorEnabled,
    NeedCreated, NeedEdited, NeedDeleted, NeedResolved,
    ResourceCreated, ResourceEdited, ResourceDeleted,
    AttachmentUploaded, AttachmentDeleted,
    MatchProposed, MatchAccepted, MatchRejected, MatchResolved
}
```

| Area           | Action               | `EntityType` | Recorded in                     | `Details`                       |
| -------------- | -------------------- | ------------ | ------------------------------- | ------------------------------- |
| Authentication | `LoginSucceeded`     | `User`       | `AuthService`                   | –                               |
|                | `LoginFailed`        | `User`       | `AuthService`                   | `Invalid credentials.`          |
|                | `TwoFactorEnabled`   | `User`       | `AuthService`                   | –                               |
| Needs          | `NeedCreated`        | `Need`       | `NeedService.CreateAsync`       | –                               |
|                | `NeedEdited`         | `Need`       | `NeedService.UpdateAsync`       | `Changed fields: Title, Region` |
|                | `NeedDeleted`        | `Need`       | `NeedService.DeleteAsync`       | –                               |
|                | `NeedResolved`       | `Need`       | `NeedService.ResolveAsync`      | –                               |
| Resources      | `ResourceCreated`    | `Resource`   | `ResourceService`               | –                               |
|                | `ResourceEdited`     | `Resource`   | `ResourceService`               | Changed field names             |
|                | `ResourceDeleted`    | `Resource`   | `ResourceService`               | –                               |
| Attachments    | `AttachmentUploaded` | `Attachment` | `AttachmentService`             | –                               |
|                | `AttachmentDeleted`  | `Attachment` | `AttachmentService`             | –                               |
| Matches        | `MatchProposed`      | `Match`      | `MatchService`                  | –                               |
|                | `MatchAccepted`      | `Match`      | `MatchService`                  | –                               |
|                | `MatchRejected`      | `Match`      | `MatchService`                  | –                               |
|                | `MatchResolved`      | `Match`      | `NeedService.ResolveAsync`      | –                               |

Edit entries list the **names** of the changed fields, never the old or new values. Values can contain personal data, such as contact points, and the current values are already stored on the record itself.

## Architecture

```text
Service (NeedService, MatchService, ...)
   │  _auditLogger.Record(AuditAction.NeedResolved, "Need", need.Id)
   ▼
IAuditLogger ── finds the actor through ICurrentUser, adds the TraceId
   │
   ▼
IAuditRepository.Add(...) ── adds to the same scoped AppDbContext
   │
   ▼
IUnitOfWork.CommitAsync() ── saves the business change and the audit entry together
```

Services call `IAuditLogger` explicitly, **after** the operation has been validated. Rejected operations (not the owner, wrong status, validation error) return before anything is recorded.

`IAuditLogger` has two overloads:

```csharp
// Normal requests: the actor is the logged-in user (from ICurrentUser).
_auditLogger.Record(AuditAction.NeedCreated, "Need", need.Id);

// Authentication: nobody is logged in yet, so the actor is passed explicitly.
_auditLogger.Record(AuditAction.LoginSucceeded, "User", user.Id, user.Id, user.Username);
```

`AuditRepository` exposes only `Add` and `GetByEntityAsync`. There is no update or delete method, so application code cannot change history.

## Transactions

A business change and its audit entry are saved through the same Unit of Work, so they are either both saved or both rolled back.

### Simple case: the entity already has an ID

```csharp
// NeedService.UpdateAsync
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    _auditLogger.Record(
        AuditAction.NeedEdited,
        "Need",
        need.Id,
        $"Changed fields: {string.Join(", ", changedFields)}");
    await _unitOfWork.CommitAsync(ct);   // saves the edited need and the audit entry
}, cancellationToken);
```

### Generated IDs: save twice inside one transaction

A new need has no `Id` until the database has inserted it. The need is therefore saved first, the audit entry is added with the generated ID, and everything is saved again, all inside one database transaction:

```csharp
// NeedService.CreateAsync
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    await _needRepository.AddAsync(need, ct);
    await _unitOfWork.CommitAsync(ct);                            // 1. INSERT need → need.Id is set
    _auditLogger.Record(AuditAction.NeedCreated, "Need", need.Id);
    await _unitOfWork.CommitAsync(ct);                            // 2. INSERT audit entry
}, cancellationToken);                                            // 3. COMMIT both, or ROLLBACK both
```

If step 2 fails, `ExecuteInTransactionAsync` rolls back and the need from step 1 disappears too. The test `CreatingNeed_RollsBackWhenAuditPersistenceFails` checks this.

### Several entries in one operation

Resolving a need writes one `NeedResolved` entry plus one `MatchResolved` entry per assigned match, all in the same transaction:

```csharp
// NeedService.ResolveAsync
foreach (var match in assignedMatches)
{
    match.Status = MatchStatus.Resolved;
    _auditLogger.Record(AuditAction.MatchResolved, "Match", match.Id);
}

foreach (var match in pendingMatches)
{
    match.Status = MatchStatus.Cancelled;   // not audited, see "Known gaps"
}

_auditLogger.Record(AuditAction.NeedResolved, "Need", need.Id);
await _unitOfWork.CommitAsync(ct);
```

### Failed logins are committed on their own

A failed login has no business change to attach to, and the entry must survive the failed attempt. It is committed immediately:

```csharp
// AuthService.LoginAsync – unknown email
_auditLogger.Record(
    AuditAction.LoginFailed,
    "User",
    null,                     // no EntityId: the account does not exist
    actorUserId: null,
    actorName: "Unknown",     // the submitted email is NOT stored
    details: "Invalid credentials.");
await _unitOfWork.CommitAsync(ct);
```

For a known account with a wrong password, `EntityId` and `ActorUserId` are the user's ID, so repeated failures against one account can be detected.

## Reading the audit history

### API

```http
GET /api/audit/{entityType}/{entityId}
```

Only authenticated `ResourceProvider` users can call it (`[Authorize(Roles = nameof(UserRole.ResourceProvider))]`). It returns a read-only `AuditLogDto` list, oldest first.

Example response for `GET /api/audit/Need/42`:

```json
[
  {
    "id": 1031,
    "occurredAt": "2026-11-06T09:12:44Z",
    "actorUserId": 7,
    "actorName": "kari.hansen",
    "action": "NeedCreated",
    "entityType": "Need",
    "entityId": 42,
    "details": null,
    "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
  },
  {
    "id": 1088,
    "occurredAt": "2026-11-06T10:03:10Z",
    "actorUserId": 7,
    "actorName": "kari.hansen",
    "action": "NeedEdited",
    "entityType": "Need",
    "entityId": 42,
    "details": "Changed fields: QuantityNeeded",
    "traceId": "00-..."
  }
]
```

### Useful SQL queries

Full history of one need. This uses the `(EntityType, EntityId)` index:

```sql
SELECT OccurredAt, ActorName, Action, Details
FROM AuditLogs
WHERE EntityType = 'Need' AND EntityId = 42
ORDER BY OccurredAt, Id;
```

Time from creation to resolution for each need (response time):

```sql
SELECT n.Id,
       n.Title,
       n.CreatedAt,
       a.OccurredAt                                 AS ResolvedAt,
       TIMESTAMPDIFF(HOUR, n.CreatedAt, a.OccurredAt) AS HoursToResolve
FROM Needs n
JOIN AuditLogs a
  ON a.EntityType = 'Need'
 AND a.EntityId   = n.Id
 AND a.Action     = 'NeedResolved'
ORDER BY HoursToResolve DESC;
```

Accounts with many failed logins in the last 24 hours. This uses the `OccurredAt` index:

```sql
SELECT ActorName, EntityId AS UserId, COUNT(*) AS FailedAttempts
FROM AuditLogs
WHERE Action = 'LoginFailed'
  AND OccurredAt >= UTC_TIMESTAMP() - INTERVAL 1 DAY
GROUP BY ActorName, EntityId
HAVING COUNT(*) >= 5
ORDER BY FailedAttempts DESC;
```

## What must never be logged

`Details` and `ActorName` must never contain:

- passwords or password hashes
- TOTP secrets or verification codes
- authentication cookies or tokens
- storage credentials
- the submitted email address of an unknown account
- field values from edits (only field names)

The test `UnknownLogin_PersistsPrivacySafeFailedAuditEntry` checks that a failed login for an unknown email does not store that email.

## Adding a new audited action

1. Add a value to `AuditAction`. It is stored as text, so no migration is needed as long as the name fits in `FieldLimits.EnumText`.
2. In the service, call `_auditLogger.Record(...)` **after** validation and **before** `CommitAsync`, inside the same `ExecuteInTransactionAsync` as the business change.
3. If the entity is new, follow the [generated IDs pattern](#generated-ids-save-twice-inside-one-transaction).
4. Keep `Details` short and free of secrets and personal data.
5. Add an integration test that checks the entry exists after success, and does **not** exist when the operation is rejected.

## Tests

| Test                                                                 | Verifies                                                            |
| -------------------------------------------------------------------- | ------------------------------------------------------------------- |
| `AuditLoggingTests.CreatingNeed_PersistsAuditEntryWithGeneratedEntityId` | The entry gets the database-generated ID of the new need.       |
| `AuditLoggingTests.UnknownLogin_PersistsPrivacySafeFailedAuditEntry`     | Failed logins are kept, without the submitted email.            |
| `AuditLoggingTests.AuditHistory_IsRestrictedToResourceProviders`         | Only `ResourceProvider` users can read the history.             |
| `AuditLoggingTests.RollingBackBusinessTransaction_RollsBackAuditEntry`   | A rolled-back business change leaves no audit entry.            |
| `AuditLoggingTests.CreatingNeed_RollsBackWhenAuditPersistenceFails`      | A failing audit entry rolls back the business change.           |
| `AuditLoggingTests.DeletingUser_SetsAuditActorToNullAndPreservesHistory` | `SET NULL`: entries survive user deletion with `ActorName` kept. |
| `ResolveNeedTests.Resolve_OwnerWithAssignedNeed_ResolvesNeedAndUpdatesMatches` | `NeedResolved` and `MatchResolved` are written on resolve.  |
| `ResolveNeedTests.Resolve_NeedNotAssigned_ReturnsConflictAndLeavesDataUnchanged` | A rejected resolve writes no entry.                       |

## Known gaps

- **Automatic cancellations are not logged.** When a need is resolved, its pending matches become `Cancelled`. When a match is accepted, competing proposals are cancelled too. There is no `MatchCancelled` action, so these changes can only be traced through the entry that caused them (`NeedResolved` or `MatchAccepted`).
- **No separate "need assigned" event.** A need becomes `Assigned` when a match is accepted. Time to assignment can be found by joining the need's matches with their `MatchAccepted` entries, but there is no single entry on the need itself.
- **The API shows one entity at a time.** There is no endpoint for "everything user X did" or "everything in the last hour". Those questions need SQL for now.

## Retention

Audit entries are historical records and are not deleted by normal application functionality. The `OccurredAt` index supports a future retention job, for example deleting entries older than a documented period. Such a job must be an explicitly authorised operational task, never ordinary user functionality.
