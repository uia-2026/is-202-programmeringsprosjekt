using System.Text.Json;
using Heimevernet.Infrastructure.Email;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Heimevernet.Infrastructure.Notifications;

/// <summary>
/// Handles the sending of emails to users when a suitable resource is registered near their open need.
/// </summary>
public sealed class SuitableResourceEmailHandler : IOutboxHandler
{
    private readonly IUserRepository _users;
    private readonly IResourceRepository _resources;
    private readonly IOutboxRepository _outbox;
    private readonly IEmailSender _sender;
    private readonly NotificationOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<SuitableResourceEmailHandler> _logger;

    public SuitableResourceEmailHandler(
        IUserRepository users,
        IResourceRepository resources,
        IOutboxRepository outbox,
        IEmailSender sender,
        IOptions<NotificationOptions> options,
        TimeProvider time,
        ILogger<SuitableResourceEmailHandler> logger)
    {
        _users = users;
        _resources = resources;
        _outbox = outbox;
        _sender = sender;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    public string Type => OutboxTypes.SuitableResourceEmail;

    public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<SuitableResourceEmailPayload>(message.Payload)
            ?? throw new InvalidOperationException("The payload is empty.");

        // A missing setting must fail visibly, so the reason shows up in LastError
        if (!Uri.TryCreate(_options.PublicBaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Notifications:PublicBaseUrl is not set to an absolute http or https address.");
        }

        if (message.RecipientUserId is not { } userId)
        {
            _logger.LogInformation("Outbox message {MessageId} skipped: no recipient", message.Id);
            return;
        }

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null || !user.EmailNotifications)
        {
            _logger.LogInformation("Outbox message {MessageId} skipped: the recipient is gone or has opted out", message.Id);
            return;
        }

        var now = _time.GetUtcNow().UtcDateTime;

        var resource = await _resources.GetByIdAsync(payload.ResourceId, ct);
        if (resource is null
            || resource.Status != ResourceStatus.Available
            || (resource.AvailableTo is { } end && end < now))
        {
            _logger.LogInformation("Outbox message {MessageId} skipped: the need is closed or past its deadline", message.Id);
            return;
        }

        var sentToday = await _outbox.CountProcessedSinceAsync(userId, Type, now.AddHours(-24), ct);
        if (sentToday >= _options.MaxEmailsPerUserPerDay)
        {
            _logger.LogInformation("Outbox message {MessageId} skipped: the daily limit is reached", message.Id);
            return;
        }

        var mail = NotificationEmails.SuitableResource(
            resource, user, payload.NeedTitle, payload.DistanceKm, _options.PublicBaseUrl);

        await _sender.SendAsync(user.Email, user.Username, mail.Subject, mail.Body, ct);
    }
}