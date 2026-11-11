namespace Heimevernet.Infrastructure.Outbox;

public interface IOutbox
{
    /// <summary>
    /// Adds a message to the current unit of work. It is saved by the next CommitAsync, in the same
    /// transaction as the change it describes. Does nothing when email is disabled.
    /// </summary>
    /// <param name="type">One of <see cref="OutboxTypes"/>.</param>
    /// <param name="payload">Small object with ids only, serialised to JSON. Never contact details.</param>
    /// <param name="dedupKey">Unique per event and recipient. Queuing the same key twice fails on commit.</param>
    /// <param name="recipientUserId">The recipient, if any. The address is looked up when the mail is sent.</param>
    void Enqueue(string type, object payload, string dedupKey, int? recipientUserId = null);
}