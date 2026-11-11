using System.Collections.Concurrent;
using Heimevernet.Infrastructure.Email;

namespace Heimevernet.IntegrationTests.Infrastructure;

/// <summary>
/// Records emails instead of sending them, so tests can assert what would have been delivered.
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent.ToArray();

    /// <summary>The emails sent to one address. Tests share the sender, so they filter by their own recipients.</summary>
    public IReadOnlyList<SentEmail> SentTo(string address) =>
        _sent.Where(e => string.Equals(e.ToAddress, address, StringComparison.OrdinalIgnoreCase)).ToList();

    public Task SendAsync(string toAddress, string toName, string subject, string textBody, CancellationToken ct = default)
    {
        _sent.Enqueue(new SentEmail(toAddress, toName, subject, textBody));
        return Task.CompletedTask;
    }
}

public sealed record SentEmail(string ToAddress, string ToName, string Subject, string Body);
