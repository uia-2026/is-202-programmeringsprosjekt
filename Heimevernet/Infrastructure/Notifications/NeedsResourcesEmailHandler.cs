using System.Text.Json;
using Heimevernet.Infrastructure.Email;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Heimevernet.Infrastructure.Notifications;

/// <summary>
/// Sends one "resources needed" email to one provider. Returns without sending when the message is stale
/// (the need is closed or past its deadline), the user is gone or has opted out, or the daily cap is reached.
/// Throws when sending fails, so the outbox retries. May run twice for the same message, which is harmless:
/// the email states a fact and links back to the application.
/// </summary>
public sealed class NeedsResourcesEmailHandler : IOutboxHandler
{
    private readonly IUserRepository _users;
    private readonly INeedRepository _needs;
    private readonly IOutboxRepository _outbox;
    private readonly IEmailSender _sender;
    private readonly NotificationOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<NeedsResourcesEmailHandler> _logger;

    /// <summary>
    /// Creates the handler with its dependencies (injected by DI).
    /// </summary>
    /// <param name="users">Repository used to load the recipient.</param>
    /// <param name="needs">Repository used to check that the need is still open.</param>
    /// <param name="outbox">Repository used to count today's emails for the daily cap.</param>
    /// <param name="sender">Sends the email.</param>
    /// <param name="options">Notification limits and the public base URL for links.</param>
    /// <param name="time">Clock, so tests can control the time.</param>
    /// <param name="logger">Logger.</param>
    public NeedsResourcesEmailHandler(
        IUserRepository users,
        INeedRepository needs,
        IOutboxRepository outbox,
        IEmailSender sender,
        IOptions<NotificationOptions> options,
        TimeProvider time,
        ILogger<NeedsResourcesEmailHandler> logger)
    {
        _users = users;
        _needs = needs;
        _outbox = outbox;
        _sender = sender;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Type => OutboxTypes.CriticalNeedEmail;

    /// <inheritdoc />
    public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<CriticalNeedEmailPayload>(message.Payload)
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

        var need = await _needs.GetByIdAsync(payload.NeedId, ct);
        if (need is null
            || need.Status is not (NeedStatus.New or NeedStatus.UnderReview)
            || (need.Deadline is { } deadline && deadline < now))
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

        var mail = NotificationEmails.CriticalNeed(
            need, user, payload.ResourceTitle, payload.DistanceKm, _options.PublicBaseUrl);

        await _sender.SendAsync(user.Email, user.Username, mail.Subject, mail.Body, ct);
    }
}