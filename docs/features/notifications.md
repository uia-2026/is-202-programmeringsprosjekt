# Email notifications

Heimevernet sends emails in two situations:

| Event                      | Who gets an email                                   | Template                              |
| -------------------------- | --------------------------------------------------- | ------------------------------------- |
| A **critical need** is registered | Owners of a fitting, available resource nearby | `NotificationEmails.CriticalNeed`     |
| A **resource** is registered      | Owners of a fitting, open need nearby (any priority) | `NotificationEmails.SuitableResource` |

Each person gets at most one email per event. For a critical need, the email names their nearest fitting resource. For a resource, it names their nearest fitting need. The email contains a link back to the application. It never contains contact details, because those are only shown to signed-in users.

## How it works

Emails are not sent while the user's request is running. The application uses a **transactional outbox**:

1. When a need or resource is created, an _event_ message (`CriticalNeedRegistered` or `ResourceRegistered`) is saved in the table `outbox_messages`, **in the same database transaction** as the need or resource. Either both are saved or neither.
2. A background worker (`OutboxWorker`) polls the table, claims due rows and calls the handler that matches each row's `type`.
3. The event handler finds the recipients and saves one _email_ message per recipient (`CriticalNeedEmail` or `SuitableResourceEmail`), again in one transaction.
4. The email handler checks that the message is still relevant, sends the email through SMTP, and the row is marked `Done`.

```
create need/resource ──► outbox_messages (event) ──► event handler ──► outbox_messages (one email per recipient) ──► email handler ──► SMTP
        └── one transaction ──┘
```

| Message type             | Handler                         | Payload                              |
| ------------------------ | ------------------------------- | ------------------------------------ |
| `CriticalNeedRegistered` | `CriticalNeedRegisteredHandler` | `CriticalNeedRegisteredPayload`      |
| `CriticalNeedEmail`      | `NeedsResourcesEmailHandler`    | `CriticalNeedEmailPayload`           |
| `ResourceRegistered`     | `ResourceRegisteredHandler`     | `ResourceRegisteredPayload`          |
| `SuitableResourceEmail`  | `SuitableResourceEmailHandler`  | `SuitableResourceEmailPayload`       |

The types are constants in `OutboxTypes`, and the payloads are records in `NotificationPayloads.cs`. Payloads hold ids and small snapshots only:

```csharp
public sealed record CriticalNeedRegisteredPayload(int NeedId);
public sealed record CriticalNeedEmailPayload(int NeedId, string ResourceTitle, double DistanceKm);

public sealed record ResourceRegisteredPayload(int ResourceId);
public sealed record SuitableResourceEmailPayload(int ResourceId, int NeedId, string NeedTitle, double DistanceKm);
```

### Step 1: queue the event with the need

`NeedService` adds the outbox message inside the same `ExecuteInTransactionAsync` call that saves the need. `IOutbox.Enqueue` only adds the row to the unit of work. Nothing is written until the next `CommitAsync`, and the database transaction is committed only when the whole block succeeds.

```csharp
await _unitOfWork.ExecuteInTransactionAsync(async ct =>
{
    await _needRepository.AddAsync(need, ct);
    await _unitOfWork.CommitAsync(ct);          // need.Id is known from here on

    _auditLogger.Record(AuditAction.NeedCreated, "Need", need.Id);

    if (need.Priority == NeedPriority.Critical)
    {
        _outbox.Enqueue(
            OutboxTypes.CriticalNeedRegistered,
            new CriticalNeedRegisteredPayload(need.Id),
            dedupKey: $"critical-need-registered:{need.Id}");
    }

    await _unitOfWork.CommitAsync(ct);
}, cancellationToken);
```

`ResourceService` does the same for every new resource, with `OutboxTypes.ResourceRegistered` and the key `resource-registered:{resourceId}`.

`OutboxWriter`, the implementation of `IOutbox`, does nothing when `Email:Enabled` is `false`, so the table does not fill up when no worker reads it. It also refuses keys longer than 128 characters and payloads larger than 2000 characters of JSON.

### Step 2: the worker claims due rows

`OutboxWorker` is a `BackgroundService`. Every `OutboxPolicy.PollInterval` (10 seconds) it asks for up to `OutboxPolicy.BatchSize` (20) due ids. A row is due when it is `Pending` and its `next_attempt_at` has passed, or when it is `Processing` but its lease has expired. Each message is then handled in its own DI scope, so a broken `DbContext` cannot affect the next message.

Before handling a row, `OutboxProcessor` claims it with one atomic `UPDATE`. The update only changes the row if it is still due, so when two workers try to claim the same row, only one of them gets `rows == 1`:

```csharp
var rows = await _context.OutboxMessages
    .Where(m => m.Id == id
             && ((m.Status == OutboxStatus.Pending && m.NextAttemptAt <= now)
              || (m.Status == OutboxStatus.Processing && m.LockedUntil < now)))
    .ExecuteUpdateAsync(s => s
        .SetProperty(m => m.Status, OutboxStatus.Processing)
        .SetProperty(m => m.LockedUntil, (DateTime?)lockedUntil)
        .SetProperty(m => m.Attempts, m => m.Attempts + 1), ct);

return rows == 1;
```

The processor then calls the handler whose `Type` matches the row. If the handler returns, the row is marked `Done`. If it throws, the row goes back to `Pending` with a later `next_attempt_at`, or to `Failed` after the last attempt:

```csharp
try
{
    if (!_handlers.TryGetValue(message.Type, out var handler))
        throw new InvalidOperationException($"No handler for outbox type '{message.Type}'.");

    await handler.HandleAsync(message, ct);
}
catch (Exception ex)
{
    // Only the message text is stored, never a stack trace
    await _repository.MarkFailedOrRetryAsync(id, message.Attempts, ex.Message, UtcNow, ct);
    return;
}

await _repository.MarkDoneAsync(id, UtcNow, ct);
```

### Step 3: the event handler fans out

`CriticalNeedRegisteredHandler` first checks that the need is still open. It then finds the recipients and queues one email per recipient. All the emails are saved in one commit:

```csharp
var recipients = await _resources.GetNotificationRecipientsAsync(
    need.CategoryId, need.Latitude, need.Longitude, need.Deadline, now,
    need.UserId, _options.CriticalNeedRadiusKm, _options.MaxRecipientsPerEvent, ct);

foreach (var recipient in recipients)
{
    _outbox.Enqueue(
        OutboxTypes.CriticalNeedEmail,
        new CriticalNeedEmailPayload(need.Id, recipient.ResourceTitle, recipient.DistanceKm),
        dedupKey: $"email-needs-resources:{need.Id}:{recipient.UserId}",
        recipientUserId: recipient.UserId);
}

try
{
    await _unitOfWork.CommitAsync(ct);
}
catch (DuplicateRecordException)
{
    // This event ran before and was cut short after saving. The batch is one
    // transaction, so its messages already exist and there is nothing more to do.
}
```

`ResourceRegisteredHandler` works the same way. It skips resources that are busy, deleted or expired, uses `INeedRepository.GetNotificationRecipientsAsync` and queues `SuitableResourceEmail` messages with the key `suitable-resource-email:{resourceId}:{userId}`.

### Step 4: the email handler sends

`NeedsResourcesEmailHandler` and `SuitableResourceEmailHandler` check everything again at send time, because the message may have waited in the queue. A handler **returns** when the message should be dropped and **throws** when it should be retried:

```csharp
// A missing setting must fail visibly, so the reason shows up in last_error
if (!Uri.TryCreate(_options.PublicBaseUrl, UriKind.Absolute, out var baseUri)
    || baseUri.Scheme is not ("http" or "https"))
{
    throw new InvalidOperationException("Notifications:PublicBaseUrl is not set to an absolute http or https address.");
}

var user = await _users.GetByIdAsync(userId, ct);
if (user is null || !user.EmailNotifications)
    return;   // removed or opted out: marked Done, nothing sent

var sentToday = await _outbox.CountProcessedSinceAsync(userId, Type, now.AddHours(-24), ct);
if (sentToday >= _options.MaxEmailsPerUserPerDay)
    return;   // daily limit reached: marked Done, nothing sent

var mail = NotificationEmails.CriticalNeed(
    need, user, payload.ResourceTitle, payload.DistanceKm, _options.PublicBaseUrl);

await _sender.SendAsync(user.Email, user.Username, mail.Subject, mail.Body, ct);   // throws on SMTP errors
```

The email address is looked up here, not stored in the payload. A user who changes their address before the email is sent gets it at the new address.

### Why an outbox

- A slow or failing mail server never slows down or breaks registering a need.
- If the application stops between saving a need and sending mail, the event is still in the database and is handled on the next start.
- Failed messages are retried with increasing delays instead of being lost.

### Delivery guarantees

Delivery is **at least once**. A message can be handled twice, for example if the application stops after sending but before marking the row `Done`. This is acceptable because an email only states a fact and links to the application.

Duplicates caused by the same event being handled twice are prevented with a unique `dedup_key`, for example `suitable-resource-email:{resourceId}:{userId}`. A second attempt to queue the same key fails on commit with `DuplicateRecordException`, and the handler treats that as "already queued".

| Message                  | Dedup key                                     |
| ------------------------ | --------------------------------------------- |
| `CriticalNeedRegistered` | `critical-need-registered:{needId}`           |
| `CriticalNeedEmail`      | `email-needs-resources:{needId}:{userId}`     |
| `ResourceRegistered`     | `resource-registered:{resourceId}`            |
| `SuitableResourceEmail`  | `suitable-resource-email:{resourceId}:{userId}` |

### Message states

| Status       | Meaning                                                                                                    |
| ------------ | ---------------------------------------------------------------------------------------------------------- |
| `Pending`    | Waiting to be processed, or waiting for the next retry                                                     |
| `Processing` | Claimed by a worker. The claim expires after a lease period, so a crashed worker does not block the message |
| `Done`       | Handled (sent or deliberately skipped)                                                                      |
| `Failed`     | Gave up after the maximum number of attempts. The reason is in `last_error`                                |

### Retries

The timing lives in `OutboxPolicy`, so the worker and the tests use the same values:

```csharp
public static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

private static readonly TimeSpan[] RetryDelays =
[
    TimeSpan.FromMinutes(1),
    TimeSpan.FromMinutes(5),
    TimeSpan.FromMinutes(30)
];

public static readonly int MaxAttempts = RetryDelays.Length + 1;   // 4
public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
public const int BatchSize = 20;
```

A message that keeps failing is therefore tried at about 0, 1, 6 and 36 minutes, and then set to `Failed`.

## When an email is not sent

The email handler skips a message (and marks it `Done`) when:

- the recipient no longer exists or has **turned off email notifications**,
- the need is resolved, deleted or past its deadline (`CriticalNeedEmail`),
- the resource is busy, deleted or expired (`SuitableResourceEmail`),
- the recipient has already received the **daily limit** of emails of that type.

It throws, so that the message is retried and the reason appears in `last_error`, when:

- `Notifications:PublicBaseUrl` is missing or not an absolute http(s) address,
- the SMTP server cannot be reached or rejects the message,
- `Email:Username` is set but `Email:Password` is missing.

The event handlers also skip early: no emails are queued for a critical need that is already closed or past its deadline, or for a resource that is already busy or expired.

## Who counts as a fitting recipient

For a new critical need (`ResourceRepository.GetNotificationRecipientsAsync`):

- resources in the same category, status `Available`, not expired,
- the resource is available no later than the need's deadline,
- inside the configured radius (bounding box in SQL, then exact distance with the Haversine formula),
- not owned by the person who registered the need,
- owner has not opted out.

For a new resource (`NeedRepository.GetNotificationRecipientsAsync`):

- open needs (`New` or `UnderReview`) in the same category with a deadline that has not passed,
- the need's deadline is not before the resource becomes available,
- inside the configured radius,
- not owned by the person who registered the resource,
- owner has not opted out.

In both cases each person appears once, with their nearest match, and the nearest people are chosen first up to `MaxRecipientsPerEvent`:

```csharp
var box = GeoMath.BoundingBox(latitude, longitude, radiusKm);

var rows = await _context.Resources
    .AsNoTracking()
    .Where(r =>
        r.CategoryId == categoryId &&
        r.Status == ResourceStatus.Available &&
        r.UserId != excludeUserId &&
        r.User.EmailNotifications &&
        (r.AvailableTo == null || r.AvailableTo >= now) &&
        (deadline == null || r.AvailableFrom <= deadline) &&
        r.Latitude >= box.MinLat && r.Latitude <= box.MaxLat &&
        r.Longitude >= box.MinLng && r.Longitude <= box.MaxLng)
    .Select(r => new { r.UserId, r.Title, r.Latitude, r.Longitude })
    .ToListAsync(cancellationToken);

return rows
    .Select(r => new { r.UserId, r.Title, Km = GeoMath.DistanceKm(latitude, longitude, r.Latitude, r.Longitude) })
    .Where(x => x.Km <= radiusKm)                    // the box is square, so measure exactly
    .GroupBy(x => x.UserId)
    .Select(g => g.OrderBy(x => x.Km).First())       // one email per person: their nearest resource
    .OrderBy(x => x.Km)
    .ThenBy(x => x.UserId)
    .Take(maxRecipients)
    .Select(x => new NotificationRecipientDto(x.UserId, x.Title, Math.Round(x.Km, 1)))
    .ToList();
```

A user is never notified about their own records.

## Email content

`NotificationEmails` builds the subject and plain-text body. The methods are pure functions, so they can be tested without a mail server:

```csharp
var mail = NotificationEmails.SuitableResource(
    resource, recipient, needTitle: "Sandbags for Storgata", distanceKm: 3.2,
    baseUrl: "https://heimevernet.example.no");

// mail.Subject: "A resource that may fit your need: <resource title>"
// mail.Body contains a link to https://heimevernet.example.no/Resource/Details/{resource.Id}
```

Text typed by users (titles, region, user name) passes through `Clean` before it is placed in a mail. It is put on one line, links are replaced with `[link removed]`, and it is shortened to a fixed length. This stops a user from using a need title to send links or fake text to other users.

## Settings

Configuration section `Notifications` (`NotificationOptions`):

| Setting                        | Default | Meaning                                                    |
| ------------------------------ | ------- | ---------------------------------------------------------- |
| `CriticalNeedRadiusKm`         | 50      | Distance from a new critical need to a resource            |
| `ResourceNotificationRadiusKm` | 50      | Distance from a new resource to a need                     |
| `MaxRecipientsPerEvent`        | 20      | Most people emailed about one event                        |
| `MaxEmailsPerUserPerDay`       | 5       | Most emails of one type one user gets in 24 hours          |
| `PublicBaseUrl`                | (none)  | Address used for links in emails                           |

Configuration section `Email` (`EmailOptions`):

| Setting                       | Meaning                                              |
| ----------------------------- | ---------------------------------------------------- |
| `Enabled`                     | When `false`, nothing is queued and the worker does not start |
| `Host`, `Port`, `UseStartTls` | SMTP server                                          |
| `Username`, `Password`        | Login for the SMTP server (not needed for Mailpit)  |
| `FromAddress`                 | Sender address. Must be verified at the provider     |

Both sections are validated at start-up. With `Email:Enabled` set to `true`, the application refuses to start if `Host`, `Port` or `FromAddress` is missing. The radii and limits must be greater than zero.

Example `appsettings.json`:

```json
{
  "Email": {
    "Enabled": true,
    "Host": "smtp-relay.brevo.com",
    "Port": 587,
    "UseStartTls": true,
    "FromAddress": "sender@example.com"
  },
  "Notifications": {
    "CriticalNeedRadiusKm": 50,
    "ResourceNotificationRadiusKm": 50,
    "MaxRecipientsPerEvent": 20,
    "MaxEmailsPerUserPerDay": 5,
    "PublicBaseUrl": "https://heimevernet.example.no"
  }
}
```

The same settings as environment variables, which is how the secrets are passed in production (a double underscore separates the section from the key):

```bash
Email__Enabled=true
Email__Host=smtp-relay.brevo.com
Email__Port=587
Email__UseStartTls=true
Email__Username=<brevo login>
Email__Password=<brevo SMTP key>
Email__FromAddress=sender@example.com
Notifications__PublicBaseUrl=https://heimevernet.example.no
```

The outbox timing (lease, retry delays, polling interval, batch size, maximum attempts) is set in code in `OutboxPolicy`, see [Retries](#retries).

## User control

Every user can turn emails off on the account page (**Email notifications**). The form posts to `AccountController.UpdateNotifications`. The setting is stored in `users.email_notifications` (default `true`) and is checked both when choosing recipients and again when the email is sent, so an opt-out made after a message was queued still counts.

## Running it

| Environment    | SMTP server                                                                                                       |
| -------------- | ----------------------------------------------------------------------------------------------------------------- |
| Local (Aspire) | Mailpit container. Emails are caught, not delivered. Open the Mailpit web page (port 8025) to read them           |
| Azure VM       | Brevo SMTP relay on port 587 with STARTTLS. Outbound port 25 is blocked on Azure, so a relay on 587 is required   |

Locally, the AppHost starts Mailpit and points the web application at it, so no setup is needed:

```csharp
var mailpit = builder.AddContainer("mailpit", "axllent/mailpit")
    .WithHttpEndpoint(port: 8025, targetPort: 8025, name: "ui")                   // the inbox in the browser
    .WithEndpoint(port: 1025, targetPort: 1025, scheme: "tcp", name: "smtp");     // SMTP, where the app sends mail

builder.AddProject<Projects.Heimevernet>("heimevernet")
    // ...
    .WaitFor(mailpit)
    .WithEnvironment("Email__Enabled", "true")
    .WithEnvironment("Email__Host", "localhost")
    .WithEnvironment("Email__Port", "1025")
    .WithEnvironment("Email__UseStartTls", "false")
    .WithEnvironment("Email__FromAddress", "noreply@heimevernet.local")
    .WithEnvironment("Notifications__PublicBaseUrl", "https://localhost:7244");
```

To try it: start the AppHost, sign in as a public actor and register a critical need near a seeded resource in the same category. Within about 10 seconds the email shows up at `http://localhost:8025`.

In production, credentials are passed as environment variables from an untracked `.env` file and are never committed.

### Checking the queue

The outbox is an ordinary table, so its state can be read with SQL. The table is created by EF Core as `OutboxMessages`:

```sql
-- How many messages are in each state
SELECT Status, COUNT(*) FROM OutboxMessages GROUP BY Status;

-- Messages that gave up, with the reason
SELECT Id, Type, Attempts, LastError, CreatedAt
FROM OutboxMessages
WHERE Status = 'Failed'
ORDER BY CreatedAt DESC;

-- Retry a failed message now
UPDATE OutboxMessages
SET Status = 'Pending', Attempts = 0, NextAttemptAt = UTC_TIMESTAMP()
WHERE Id = 42;
```

## Adding a new notification type

1. Add a constant to `OutboxTypes` and a payload record to `NotificationPayloads.cs`. Keep the payload to ids and short snapshots.
2. Call `_outbox.Enqueue(...)` inside the transaction that makes the change, with a dedup key that is unique per event and recipient.
3. Write a handler that implements `IOutboxHandler`. Return to drop a message, throw to retry it, and make sure running it twice is harmless.
4. Register the handler in `EmailExtensions.AddEmail`.

```csharp
public sealed class MatchAcceptedEmailHandler : IOutboxHandler
{
    public string Type => OutboxTypes.MatchAcceptedEmail;

    public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<MatchAcceptedEmailPayload>(message.Payload)
            ?? throw new InvalidOperationException("The payload is empty.");

        // 1. Load the current data and return if the message is stale or the user opted out
        // 2. Build the mail with a pure function in NotificationEmails
        // 3. await _sender.SendAsync(...), which throws on failure so the message is retried
    }
}

// EmailExtensions.AddEmail
services.AddScoped<IOutboxHandler, MatchAcceptedEmailHandler>();
```

`OutboxProcessor` builds a dictionary of handlers by `Type`, so registering two handlers for the same type fails on the worker's first pass instead of sending twice.

## Limitations

- Emails are sent only for new needs and resources. Changing a need's priority to critical later does not send a notification.
- The sender address is a free-mail address, so some receivers may mark the emails as spam. A domain with DKIM and DMARC records would fix this.
- The free Brevo plan has a daily sending limit and adds its own footer.
- The recipient search is a bounding-box query followed by an in-memory distance filter. That is fine for the current data size but would need a spatial index for very large data.
- Skipped messages are also stored as `Done`, so they count toward the daily limit.
