using System.Text.Json;
using Heimevernet.Infrastructure.Email;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace Heimevernet.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutbox
{
    private readonly IOutboxRepository _repository;
    private readonly EmailOptions _email;

    public OutboxWriter(IOutboxRepository repository, IOptions<EmailOptions> email)
    {
        _repository = repository;
        _email = email.Value;
    }

    public void Enqueue(string type, object payload, string dedupKey, int? recipientUserId = null)
    {
        // Nothing reads the outbox when email is off, so the table would only fill up
        if (!_email.Enabled) return;

        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(dedupKey);
        ArgumentNullException.ThrowIfNull(payload);

        if (dedupKey.Length > FieldLimits.DedupKey)
            throw new ArgumentException($"The dedup key is longer than {FieldLimits.DedupKey} characters.", nameof(dedupKey));

        var json = JsonSerializer.Serialize(payload);
        if (json.Length > FieldLimits.OutboxPayload)
            throw new ArgumentException("The payload is too large. Store ids only.", nameof(payload));

        _repository.Add(new OutboxMessage
        {
            Type = type,
            DedupKey = dedupKey,
            RecipientUserId = recipientUserId,
            Payload = json
        });
    }
}