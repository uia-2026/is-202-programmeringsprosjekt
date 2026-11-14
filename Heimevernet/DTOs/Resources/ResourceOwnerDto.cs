namespace Heimevernet.DTOs.Resources;

/// <summary>
/// A user who owns at least one Resource. Used to populate the owner filter (#37).
/// </summary>
/// <param name="UserId">ID of the owner.</param>
/// <param name="Username">Username of the owner.</param>
public record ResourceOwnerDto(int UserId, string Username);