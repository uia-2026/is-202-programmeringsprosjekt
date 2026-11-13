namespace Heimevernet.Infrastructure.Notifications;

/// <summary>Payload of an CriticalNeedEmail outbox message. Ids and a snapshot of the resource title.</summary>
/// <param name="NeedId">ID of the critical need.</param>
/// <param name="ResourceTitle">Title of the recipient's nearest fitting resource when the event ran.</param>
/// <param name="DistanceKm">Distance from that resource to the need, in kilometres.</param>
public sealed record CriticalNeedEmailPayload(int NeedId, string ResourceTitle, double DistanceKm);

/// <summary>Payload of a Critical NeedRegistered outbox message.</summary>
/// <param name="NeedId">ID of the critical need that was registered.</param>
public sealed record CriticalNeedRegisteredPayload(int NeedId);

/// <summary>Payload of a ResourceRegistered outbox message.</summary>
/// <param name="ResourceId">ID of the resource that was registered.</param>
public sealed record ResourceRegisteredPayload(int ResourceId);

/// <summary>Payload of a SuitableResourceEmail outbox message: ids and a snapshot of the recipient's need title.</summary>
/// <param name="ResourceId">ID of the new resource.</param>
/// <param name="NeedId">ID of the recipient's nearest open need.</param>
/// <param name="NeedTitle">Title of that need when the event ran.</param>
/// <param name="DistanceKm">Distance from the resource to the need, in kilometres.</param>
public sealed record SuitableResourceEmailPayload(int ResourceId, int NeedId, string NeedTitle, double DistanceKm);