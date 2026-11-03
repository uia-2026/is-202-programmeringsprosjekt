# Concurrency handling

Heimevernet is used by many people at the same time. Two users can open the same Need or Resource, and both can try to change it. This page explains how the application keeps data correct when that happens.

We use **optimistic concurrency control** with an integer `Version` column.

## The problem

The matching flow is a _read, check, write_ sequence. The check depends on quantities that other users may change in between.

```text
User A (Need owner)                User B (Need owner, same Need)
-------------------                ------------------------------
Reads Need (needs 5, 0 allocated)  Reads Need (needs 5, 0 allocated)
Checks: proposing 5 <= 5  OK       Checks: proposing 5 <= 5  OK
Creates Match of 5                 Creates Match of 5
Commit                             Commit
                                   -> Need now has 10 proposed, but only 5 needed
```

Without protection, the second write silently overwrites or ignores the first (a _lost update_). The same applies when two Resource Providers accept matches against the same Resource or Need at the same time.

## Why optimistic concurrency

| Approach                   | Description                                        | Why (not)                                                                 |
| -------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------- |
| Pessimistic locking        | Lock rows while a user works                       | Holds locks during slow user actions, hurts throughput, risk of deadlocks |
| **Optimistic concurrency** | Let everyone read; detect a conflict at write time | Conflicts are rare in this domain, no locks held, simple to reason about  |

When a conflict is detected, the second writer is told that the data changed and can try again.

## Overview

Four parts work together:

1. **`IVersioned`** – marker interface for entities with a `Version` token.
2. **EF Core configuration** – registers `Version` as a concurrency token.
3. **`SaveChangesAsync` override** – increments `Version` on every modified entity.
4. **`MarkForConcurrencyCheck`** – forces a version check on entities that would otherwise not be written.

The service layer catches `ConcurrencyConflictException` and returns a result such as `ProposeMatchResult.ConcurrentChange` or `MatchDecisionResult.ConcurrentChange`.

## 1. The `IVersioned` interface

```csharp
namespace Heimevernet.Models.Interfaces;

public interface IVersioned
{
    int Version { get; set; }
}
```

`Need`, `Resource` and `Match` implement it:

```csharp
public class Need : IVersioned
{
    public int Id { get; set; }
    // ... other properties ...

    // Concurrency token for optimistic concurrency control.
    public int Version { get; set; }
}
```

Using an interface lets the `DbContext` handle all versioned entities in one place, without knowing about each concrete type.

## 2. EF Core configuration

In `OnModelCreating`, the `Version` property is registered as a concurrency token:

```csharp
modelBuilder.Entity<Need>().Property(n => n.Version).IsConcurrencyToken();
modelBuilder.Entity<Resource>().Property(r => r.Version).IsConcurrencyToken();
modelBuilder.Entity<Match>().Property(m => m.Version).IsConcurrencyToken();
```

For every `UPDATE`, EF Core now adds the version that was originally loaded to the `WHERE` clause:

```sql
UPDATE Needs
SET Status = @p0, Version = @p1
WHERE Id = @p2 AND Version = @p3;   -- @p3 is the version we read
```

If another user has already changed the row, its version no longer matches. **Zero rows are updated**, and EF Core throws a `DbUpdateConcurrencyException`. The unit of work translates this into our own `ConcurrencyConflictException`, so the service layer does not depend on EF Core types.

## 3. Incrementing the version automatically

We use a plain `int` and increment it in code, rather than a database-generated `rowversion`. This works the same on every database provider.

```csharp
public override Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries<IVersioned>()
                 .Where(e => e.State == EntityState.Modified))
    {
        entry.Entity.Version++;
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

How it works:

- Only entities in state `Modified` are touched. New entities start at `0`.
- `Version++` changes the **current** value. EF Core still remembers the **original** value, which is what ends up in the `WHERE` clause.
- Developers never increment `Version` manually. Any modification to a versioned entity is protected automatically.

## 4. Forcing a check with `MarkForConcurrencyCheck`

EF Core only writes entities that actually changed. This creates a gap: sometimes a decision is _based on_ an entity, but that entity is not modified.

Example: when a Need is already `UnderReview` and a second proposal is created, only a new `Match` row is inserted. The Need is untouched, so no version check happens, and two users can both pass the quantity check.

The repositories solve this by marking the `Version` property as modified:

```csharp
public void MarkForConcurrencyCheck(Need need)
{
    _context.Entry(need)
        .Property(n => n.Version)
        .IsModified = true;
}

public void MarkForConcurrencyCheck(Resource resource)
{
    _context.Entry(resource)
        .Property(r => r.Version)
        .IsModified = true;
}
```

This puts the entity in state `Modified`, so:

1. `SaveChangesAsync` increments its `Version`.
2. EF Core issues an `UPDATE ... WHERE Version = @original`.
3. If someone else changed the row first, the update affects zero rows and the commit fails.

In short: _"I relied on this row being unchanged. Fail if it changed."_

## Where it is used

All three operations live in `MatchService`.

| Operation      | Entities version-checked                                     | Why                                                                                                     |
| -------------- | ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------- |
| `ProposeAsync` | `Need`                                                       | Two people must not propose matches on the same Need at the same time and exceed the remaining quantity |
| `AcceptAsync`  | `Need`, `Resource`, every affected Need of cancelled matches | Two providers must not accept matches on the same Need or Resource at the same time                     |
| `RejectAsync`  | `Need`                                                       | Prevents concurrent operations from overwriting the Need status                                         |

`Match` rows are versioned too. Their `Version` is incremented automatically whenever their status changes (for example to `Assigned`, `Rejected` or `Cancelled`), so no explicit marking is needed.

### Propose

```csharp
await _matchRepository.AddAsync(match, cancellationToken);

if (need.Status == NeedStatus.New)
{
    need.Status = NeedStatus.UnderReview;
}

// Mark the Need for a version check, so two people cannot propose
// matches on the same need at the same time.
_needRepository.MarkForConcurrencyCheck(need);

try
{
    await _unitOfWork.CommitAsync(cancellationToken);
}
catch (DuplicateRecordException)
{
    return (ProposeMatchResult.MatchAlreadyExists, null, null);
}
catch (ConcurrencyConflictException)
{
    return (ProposeMatchResult.ConcurrentChange, null, null);
}
```

`DuplicateRecordException` handles a related race: two identical proposals for the same Need and Resource. A unique constraint in the database catches it, even if both passed the `ExistsAsync` check.

### Accept

Accepting is the most complex operation. It can change the Need, the Resource, and cancel other pending proposals, which in turn changes _other_ Needs. Everything that was read or modified is protected:

```csharp
// Recalculate the status of other Needs affected by cancelled proposals.
foreach (var needId in matchesToCancel.Values.Select(m => m.NeedId).Distinct())
{
    // ... recalculate affectedNeed.Status ...
    _needRepository.MarkForConcurrencyCheck(affectedNeed);
}

// Force a version check on Need and Resource, so two people cannot
// accept matches on the same need or resource at the same time.
_needRepository.MarkForConcurrencyCheck(match.Need);
_resourceRepository.MarkForConcurrencyCheck(match.Resource);

try
{
    await _unitOfWork.CommitAsync(cancellationToken);
}
catch (ConcurrencyConflictException)
{
    return MatchDecisionResult.ConcurrentChange;
}
```

Everything is saved in **one commit**, so either all changes apply or none do.

### Reject

```csharp
match.Status = MatchStatus.Rejected;

// ... decide whether the Need goes back to New or stays UnderReview ...

// Prevents concurrent operations from overwriting Need status changes.
_needRepository.MarkForConcurrencyCheck(match.Need);
```

The status of the Need is derived from other rows (allocated quantity and pending matches). If those change while we calculate, the version check makes the commit fail instead of saving a stale status.

## Example: two simultaneous accepts

```text
Resource R offers 10. Match M1 asks 6, Match M2 asks 6 (both pending).

Provider (M1)                         Provider (M2)
-------------                         -------------
Load M1, Need, Resource (v=3)         Load M2, Need, Resource (v=3)
Remaining 10 >= 6  OK                 Remaining 10 >= 6  OK
Resource.Version = 4                  Resource.Version = 4
UPDATE ... WHERE Version = 3  -> 1 row
                                      UPDATE ... WHERE Version = 3  -> 0 rows
                                      ConcurrencyConflictException
                                      -> MatchDecisionResult.ConcurrentChange
```

Only one accept succeeds. The other user gets `ConcurrentChange` and nothing is saved for them.

## Handling the result in the UI

The service never throws a concurrency error to the controller. It returns a result enum:

```csharp
var result = await _matchService.AcceptAsync(matchId, userId, cancellationToken);

if (result == MatchDecisionResult.ConcurrentChange)
{
    // Show a friendly message and reload the page.
    TempData["Error"] = "This match was changed by someone else. Please review the latest data and try again.";
    return RedirectToAction(nameof(Index));
}
```

There is **no automatic retry**. After a conflict the situation may have changed (for example the Need is now fully assigned), so the user should see fresh data and decide again.

## Limits and good to know

- The version check protects _rows we marked or modified_. If a decision depends on a row, mark that row with `MarkForConcurrencyCheck`.
- Quantity sums (`GetAllocatedQuantityFor...`) are read outside any lock. Correctness comes from the version check on the `Need` and `Resource` that guard those sums, not from the read itself.
- A conflict is expected to be rare. If it becomes frequent, consider a retry policy or finer-grained locking.
- Version values only ever go up, and `Version` is not shown to users.

## Summary

| Piece                          | Responsibility                                                     |
| ------------------------------ | ------------------------------------------------------------------ |
| `IVersioned`                   | Marks entities that carry a `Version` token                        |
| `IsConcurrencyToken()`         | Adds `AND Version = @original` to updates                          |
| `SaveChangesAsync` override    | Increments `Version` on modified entities                          |
| `MarkForConcurrencyCheck`      | Forces a check on entities that are read but not otherwise changed |
| `ConcurrencyConflictException` | Domain-level signal that someone else changed the data             |
| `*Result.ConcurrentChange`     | Tells the UI to show a message and reload                          |
