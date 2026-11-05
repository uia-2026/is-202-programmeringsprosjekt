using Heimevernet.Extensions;
using Heimevernet.Services.Interfaces;
using System.Security.Claims;

namespace Heimevernet.Services;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const string AnonymousActorName = "Anonymous";

    private ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    public int? UserId
    {
        get
        {
            if (!(Principal.Identity?.IsAuthenticated ?? false))
                return null;

            return Principal.GetUserId();
        }
    }

    public string ActorName =>
        Principal.Identity?.IsAuthenticated == true
            ? Principal.FindFirstValue(ClaimTypes.Name) ?? AnonymousActorName
            : AnonymousActorName;
}
