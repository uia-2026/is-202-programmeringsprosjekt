namespace Heimevernet.DTOs.Notifications;

/// <summary>A provider who should hear about a need, with their nearest fitting resource. No contact details.</summary>
/// <param name="UserId">ID of the provider to notify.</param>
/// <param name="ResourceTitle">Title of the provider's nearest fitting resource, used in the email.</param>
/// <param name="DistanceKm">Distance from the need to the resource, in kilometres.</param>
public sealed record NotificationRecipientDto(int UserId, string ResourceTitle, double DistanceKm);