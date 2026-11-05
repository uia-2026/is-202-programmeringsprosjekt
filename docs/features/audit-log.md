# Audit logging

## Purpose

The application records important authentication and domain changes in an append-only audit history. Audit entries provide a traceable record without storing credentials or other authentication secrets.

## Data model

`AuditLog` contains:

- `Id`: database-generated audit identifier.
- `OccurredAt`: UTC timestamp.
- `ActorUserId`: optional reference to the user who performed the action.
- `ActorName`: actor-name snapshot retained if the user is renamed or deleted.
- `Action`: `AuditAction` value stored as text.
- `EntityType` and `EntityId`: the affected entity, when available.
- `Details`: short, non-sensitive event details.
- `TraceId`: request or activity trace identifier when available.

The database indexes `(EntityType, EntityId)` for entity history queries and `OccurredAt` for chronological queries. The optional actor relationship uses `DeleteBehavior.SetNull`, so deleting a user does not delete historical audit records.

## Recorded actions

The application records:

- Authentication: `LoginSucceeded`, `LoginFailed`, `TwoFactorEnabled`.
- Needs: `NeedCreated`, `NeedEdited`, `NeedResolved`.
- Resources: `ResourceCreated`, `ResourceEdited`, `ResourceDeleted`.
- Attachments: `AttachmentUploaded`, `AttachmentDeleted`.
- Matches: `MatchProposed`, `MatchAccepted`, `MatchRejected`, `MatchResolved`.

Edit entries contain changed field names only. They do not contain previous or new field values.

## Architecture

Application services make explicit calls to `IAuditLogger` after validating the corresponding operation. `IAuditLogger` obtains the actor from `ICurrentUser` for normal requests, or accepts an explicit actor for authentication events. The current implementation keeps the existing claims extensions and uses `Activity.Current?.Id` or the current HTTP trace identifier for correlation.

`IAuditLogger` adds entries through `IAuditRepository`. The repository uses the same scoped `AppDbContext` as the existing Unit of Work and exposes no update or delete operation.

## Transactions

Business changes and their audit entries are saved through the same Unit of Work. Operations with database-generated IDs flush the business entity inside an explicit database transaction, add the audit entry with the generated ID, and then save both changes before committing. If the operation fails, the transaction rolls back the business change and its audit entries together.

Failed login attempts are independent events because authentication fails. They are committed separately so that the failed attempt remains auditable. Unknown accounts use a null actor ID and a generic actor name; the submitted email address is not stored.

Resolving a need records `NeedResolved` and one `MatchResolved` for every affected assigned match in the same transaction.

## Access control and append-only behavior

Audit history is exposed through `GET /api/audit/{entityType}/{entityId}` and is restricted to authenticated `ResourceProvider` users. The endpoint returns a read-only DTO and no application endpoint exposes audit update or delete operations.

Audit details must never contain passwords, password hashes, TOTP secrets, verification codes, authentication tokens, storage credentials, submitted email addresses for unknown accounts, or other secrets. All timestamps are stored in UTC.

## Retention

Audit entries are historical records and are not deleted by normal application functionality. The `OccurredAt` index supports future retention queries. Any retention process must be an explicitly authorized operational task with a documented retention period and must not be exposed as ordinary user functionality.
