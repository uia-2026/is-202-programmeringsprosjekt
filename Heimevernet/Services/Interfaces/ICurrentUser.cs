namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Gives access to the user making the current HTTP request, based on the authentication cookie.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// ID of the signed-in user, or <see langword="null"/> for anonymous requests.
    /// </summary>
    int? UserId { get; }
    /// <summary>
    /// Username of the signed-in user, or "Anonymous" when nobody is signed in. Used in the audit log.
    /// </summary>
    string ActorName { get; }
}