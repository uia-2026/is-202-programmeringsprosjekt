/// <summary>One public actor to notify about a new resource, with their nearest suitable need.</summary>
/// <param name="UserId">ID of the user to notify.</param>
/// <param name="NeedId">ID of the user's nearest suitable need.</param>
/// <param name="NeedTitle">Title of that need, used in the email.</param>
/// <param name="DistanceKm">Distance from the resource to the need, in kilometres.</param>
public sealed record NeedNotificationRecipientDto(int UserId, int NeedId, string NeedTitle, double DistanceKm);