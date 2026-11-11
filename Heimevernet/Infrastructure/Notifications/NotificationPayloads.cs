namespace Heimevernet.Infrastructure.Notifications;

/// <summary>Payload of an CriticalNeedEmail outbox message. Ids and a snapshot of the resource title.</summary>
public sealed record CriticalNeedEmailPayload(int NeedId, string ResourceTitle, double DistanceKm);

/// <summary>Payload of a Critical NeedRegistered outbox message.</summary>
public sealed record CriticalNeedRegisteredPayload(int NeedId);

/// <summary>Payload of a ResourceRegistered outbox message.</summary>
public sealed record ResourceRegisteredPayload(int ResourceId);

/// <summary>Payload of a SuitableResourceEmail outbox message: ids and a snapshot of the recipient's need title.</summary>
public sealed record SuitableResourceEmailPayload(int ResourceId, int NeedId, string NeedTitle, double DistanceKm);