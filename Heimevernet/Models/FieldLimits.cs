namespace Heimevernet.Models;

/// <summary>
/// Maximum lengths shared by validation attributes and database columns, so the two cannot drift apart.
/// </summary>
public static class FieldLimits
{
    /// <summary>Max length of a username.</summary>
    public const int Username = 50;
    /// <summary>Max length of an email address (the limit in RFC 5321).</summary>
    public const int Email = 254;
    /// <summary>Max length of a stored password hash.</summary>
    public const int PasswordHash = 256;
    /// <summary>Max length of the actor type text, e.g. "Municipality".</summary>
    public const int ActorType = 100;
    /// <summary>Max length of a stored 2FA secret.</summary>
    public const int TwoFactorSecret = 128;

    /// <summary>Max length of a need or resource title.</summary>
    public const int Title = 150;
    /// <summary>Max length of a need or resource description.</summary>
    public const int Description = 1000;
    /// <summary>Max length of a region name.</summary>
    public const int Region = 100;
    /// <summary>Max length of contact details on a need or resource.</summary>
    public const int ContactPoint = 200;
    /// <summary>Highest allowed quantity on a need, resource or match.</summary>
    public const int MaxQuantity = 10_000;

    /// <summary>Max length of a category name.</summary>
    public const int CategoryName = 100;
    /// <summary>Max length of an object storage key.</summary>
    public const int StorageKey = 512;
    /// <summary>Max length of enum values stored as text.</summary>
    public const int EnumText = 32;
    /// <summary>Max length of <see cref="AuditLog.ActorName"/>.</summary>
    public const int AuditActorName = 100;
    /// <summary>Max length of <see cref="AuditLog.EntityType"/>.</summary>
    public const int AuditEntityType = 100;
    /// <summary>Max length of <see cref="AuditLog.Details"/>.</summary>
    public const int AuditDetails = 500;
    /// <summary>Max length of <see cref="AuditLog.TraceId"/>.</summary>
    public const int AuditTraceId = 200;

    /// <summary>Max length of <see cref="OutboxMessage.DedupKey"/>.</summary>
    public const int DedupKey = 128;
    /// <summary>Max length of <see cref="OutboxMessage.Payload"/>.</summary>
    public const int OutboxPayload = 2000;
    /// <summary>Max length of <see cref="OutboxMessage.LastError"/>. Longer errors are cut.</summary>
    public const int OutboxError = 1000;
}