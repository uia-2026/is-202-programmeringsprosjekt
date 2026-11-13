using System.Text.Json;
using Heimevernet.Exceptions;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Heimevernet.Infrastructure.Notifications;

/// <summary>
/// Turns a CriticalNeedRegistered event into one email message per provider who has a fitting resource nearby.
/// Skips needs that are no longer open.
/// </summary>
public sealed class CriticalNeedRegisteredHandler : IOutboxHandler
{
    private readonly INeedRepository _needs;
    private readonly IResourceRepository _resources;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NotificationOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<CriticalNeedRegisteredHandler> _logger;

    /// <summary>
    /// Creates the handler with its dependencies (injected by DI).
    /// </summary>
    /// <param name="needs">Repository used to load the need.</param>
    /// <param name="resources">Repository used to find providers to notify.</param>
    /// <param name="outbox">Queues the per-recipient email messages.</param>
    /// <param name="unitOfWork">Saves the queued messages.</param>
    /// <param name="options">Notification radius and limits.</param>
    /// <param name="time">Clock, so tests can control the time.</param>
    /// <param name="logger">Logger.</param>
    public CriticalNeedRegisteredHandler(
        INeedRepository needs,
        IResourceRepository resources,
        IOutbox outbox,
        IUnitOfWork unitOfWork,
        IOptions<NotificationOptions> options,
        TimeProvider time,
        ILogger<CriticalNeedRegisteredHandler> logger)
    {
        _needs = needs;
        _resources = resources;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Type => OutboxTypes.CriticalNeedRegistered;

    /// <inheritdoc />
    public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<CriticalNeedRegisteredPayload>(message.Payload)
            ?? throw new InvalidOperationException("The payload is empty.");

        var now = _time.GetUtcNow().UtcDateTime;

        var need = await _needs.GetByIdAsync(payload.NeedId, ct);
        if (need is null
            || need.Status is not (NeedStatus.New or NeedStatus.UnderReview)
            || (need.Deadline is { } deadline && deadline < now))
        {
            _logger.LogInformation("Need {NeedId}: no notifications, it is closed, gone or past its deadline", payload.NeedId);
            return;
        }

        var recipients = await _resources.GetNotificationRecipientsAsync(
            need.CategoryId, need.Latitude, need.Longitude, need.Deadline, now,
            need.UserId, _options.CriticalNeedRadiusKm, _options.MaxRecipientsPerEvent, ct);

        if (recipients.Count == 0)
        {
            _logger.LogInformation("Need {NeedId}: no providers to notify", need.Id);
            return;
        }

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
            _logger.LogInformation("Need {NeedId}: queued {Count} notification email(s)", need.Id, recipients.Count);
        }
        catch (DuplicateRecordException)
        {
            // This event ran before and was cut short after saving. The batch is one transaction,
            // so its messages already exist and there is nothing more to do.
            _logger.LogInformation("Need {NeedId}: notifications were already queued", need.Id);
        }
    }
}