using System.Text.Json;
using Heimevernet.Exceptions;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Heimevernet.Infrastructure.Notifications;

/// <summary>
/// Turns a ResourceRegistered event into one email message per provider who has a fitting need nearby.
/// </summary>
public sealed class ResourceRegisteredHandler : IOutboxHandler
{
    private readonly INeedRepository _needs;
    private readonly IResourceRepository _resources;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NotificationOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<ResourceRegisteredHandler> _logger;

    /// <summary>
    /// Creates the handler with its dependencies (injected by DI).
    /// </summary>
    /// <param name="needs">Repository used to find users with fitting needs.</param>
    /// <param name="resources">Repository used to load the resource.</param>
    /// <param name="outbox">Queues the per-recipient email messages.</param>
    /// <param name="unitOfWork">Saves the queued messages.</param>
    /// <param name="options">Notification radius and limits.</param>
    /// <param name="time">Clock, so tests can control the time.</param>
    /// <param name="logger">Logger.</param>
    public ResourceRegisteredHandler(
        INeedRepository needs,
        IResourceRepository resources,
        IOutbox outbox,
        IUnitOfWork unitOfWork,
        IOptions<NotificationOptions> options,
        TimeProvider time,
        ILogger<ResourceRegisteredHandler> logger)
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
    public string Type => OutboxTypes.ResourceRegistered;

    /// <inheritdoc />
    public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ResourceRegisteredPayload>(message.Payload)
            ?? throw new InvalidOperationException("The payload is empty.");

        var now = _time.GetUtcNow().UtcDateTime;

        var resource = await _resources.GetByIdAsync(payload.ResourceId, ct);
        if (resource is null
            || resource.Status != ResourceStatus.Available
            || (resource.AvailableTo is { } end && end < now))
        {
            _logger.LogInformation("Resource {ResourceId}: no notifications, it is busy, gone or expired", payload.ResourceId);
            return;
        }

        var recipients = await _needs.GetNotificationRecipientsAsync(
            resource.CategoryId, resource.Latitude, resource.Longitude,
            resource.AvailableFrom, resource.AvailableTo, now,
            resource.UserId, _options.ResourceNotificationRadiusKm, _options.MaxRecipientsPerEvent, ct);

        if (recipients.Count == 0)
        {
            _logger.LogInformation("Resource {ResourceId}: no providers to notify", resource.Id);
            return;
        }

        foreach (var recipient in recipients)
        {
            _outbox.Enqueue(
                OutboxTypes.SuitableResourceEmail,
                new SuitableResourceEmailPayload(resource.Id, recipient.NeedId, recipient.NeedTitle, recipient.DistanceKm),
                dedupKey: $"suitable-resource-email:{resource.Id}:{recipient.UserId}",
                recipientUserId: recipient.UserId);
        }

        try
        {
            await _unitOfWork.CommitAsync(ct);
            _logger.LogInformation("Resource {ResourceId}: queued {Count} notification email(s)", resource.Id, recipients.Count);
        }
        catch (DuplicateRecordException)
        {
            // This event ran before and was cut short after saving. The batch is one transaction,
            // so its messages already exist and there is nothing more to do.
            _logger.LogInformation("Resource {ResourceId}: notifications were already queued", resource.Id);
        }
    }
}