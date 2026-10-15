using System.Security.Claims;
using Heimevernet.Models;

namespace Heimevernet.Extensions;

/// <summary>
/// Extension methods for reading common claims off the current ClaimsPrincipal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim, out var userId))
            throw new InvalidOperationException("User is not authenticated or missing a valid user ID claim.");

        return userId;
    }

    public static UserRole GetRole(this ClaimsPrincipal user) =>
        Enum.TryParse<UserRole>(user.FindFirstValue(ClaimTypes.Role), out var role)
        ? role
        : throw new InvalidOperationException("User has no valid role claim.");
}