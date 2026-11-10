namespace Heimevernet.DTOs.Notifications;

/// <summary>A provider who should hear about a need, with their nearest fitting resource. No contact details.</summary>
public sealed record NotificationRecipientDto(int UserId, string ResourceTitle, double DistanceKm);