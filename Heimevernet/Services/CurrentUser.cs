using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using System.Security.Claims;

namespace Heimevernet.Services;

/// <summary>
/// Default <see cref="ICurrentUser"/> that reads the user from the current <see cref="HttpContext"/>.
/// </summary>
/// <param name="httpContextAccessor">Gives access to the current request.</param>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const string AnonymousActorName = "Anonymous";

    private ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    /// <inheritdoc />
    public int? UserId
    {
        get
        {
            if (!(Principal.Identity?.IsAuthenticated ?? false))
                return null;

            return Principal.GetUserId();
        }
    }

    /// <inheritdoc />
    public string ActorName =>
        Principal.Identity?.IsAuthenticated == true
            ? Principal.FindFirstValue(ClaimTypes.Name) ?? AnonymousActorName
            : AnonymousActorName;
}