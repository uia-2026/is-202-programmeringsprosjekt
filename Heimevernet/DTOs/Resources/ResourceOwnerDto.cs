namespace Heimevernet.DTOs.Resources;

/// <summary>
/// A user who owns at least one Resource. Used to populate the owner filter (#37).
/// </summary>
public record ResourceOwnerDto(int UserId, string Username);
