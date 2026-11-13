using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Extensions;

/// <summary>
/// Startup registration for authentication and authorization.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Registers cookie authentication, the authenticated-by-default authorization policy and the auth service.
    /// </summary>
    /// <remarks>
    /// Uses an 8-hour cookie (HttpOnly, Secure, SameSite=Lax). The fallback policy means every
    /// endpoint needs a signed-in user unless it has <c>[AllowAnonymous]</c>.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same <paramref name="services"/>, so calls can be chained.</returns>
    public static IServiceCollection AddAppAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                // Redirect unauthenticated users to the login page.
                options.LoginPath = "/Account/Login";

                // Redirect authenticated users who do not have permission.
                options.AccessDeniedPath = "/Account/AccessDenied";

                // Limit how long an authentication session remains valid.
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;

                // Protect the authentication cookie.
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}