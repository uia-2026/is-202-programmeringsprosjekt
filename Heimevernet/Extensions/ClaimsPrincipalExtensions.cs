using System.Security.Claims;
using Heimevernet.Models;

namespace Heimevernet.Extensions;

/// <summary>
/// Extension methods for reading common claims off the current ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the user ID from the <see cref="ClaimTypes.NameIdentifier"/> claim.
    /// </summary>
    /// <param name="user">The signed-in user.</param>
    /// <returns>The user's ID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the claim is missing or not a number, e.g. for anonymous users.</exception>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim, out var userId))
            throw new InvalidOperationException("User is not authenticated or missing a valid user ID claim.");

        return userId;
    }

    /// <summary>
    /// Reads the user's role from the <see cref="ClaimTypes.Role"/> claim.
    /// </summary>
    /// <param name="user">The signed-in user.</param>
    /// <returns>The user's <see cref="UserRole"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the claim is missing or not a known role.</exception>
    public static UserRole GetRole(this ClaimsPrincipal user) =>
        Enum.TryParse<UserRole>(user.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : throw new InvalidOperationException("User has no valid role claim.");
}