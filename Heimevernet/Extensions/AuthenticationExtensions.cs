using Heimevernet.Models;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Registers cookie authentication, the authenticated-by-default authorization policy and the auth service.
    /// </summary>
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

        services.AddAuthorization(options =>
        {
            // Require authentication for all endpoints by default.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}