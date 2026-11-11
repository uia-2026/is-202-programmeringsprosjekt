namespace Heimevernet.Infrastructure.Outbox;

public static class OutboxTypes
{
    /// <summary>Fan-out event: a critical need was registered. The worker turns it into one email per recipient.</summary>
    public const string CriticalNeedRegistered = "CriticalNeedRegistered";

    /// <summary>One email to one provider about one critical need.</summary>
    public const string CriticalNeedEmail = "CriticalNeedEmail";

    /// <summary>Fan-out event: a resource was registered. The worker turns it into one email per recipient.</summary>
    public const string ResourceRegistered = "ResourceRegistered";

    /// <summary>One email to public actor about suitable resource registered.</summary>
    public const string SuitableResourceEmail = "SuitableResourceEmail";
}