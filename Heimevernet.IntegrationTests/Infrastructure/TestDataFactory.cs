using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;

namespace Heimevernet.IntegrationTests.Infrastructure;

/// <summary>
/// Factory for seeding predictable test users and database entities into the integration test environment.
/// </summary>
public sealed class TestDataFactory
{
    private readonly IServiceProvider _services;

    public TestDataFactory(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Seeds a new <see cref="User"/> into the MariaDB test database with uniquely generated email/username,
    /// properly hashed password credentials, and optional 2FA activation.
    /// </summary>
    /// <param name="twoFactorEnabled">Determines whether 2FA and a TOTP secret key should be pre-configured for the user.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The persisted <see cref="User"/> entity.</returns>
    public async Task<User> CreateUserAsync(
        bool twoFactorEnabled = false,
        CancellationToken cancellationToken = default)
    {
        // Use an isolated DI scope to prevent tracked entity caching side-effects across tests.
        using var scope = _services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var suffix = Guid.NewGuid().ToString("N");

        // Generate a 20-byte Base32 TOTP secret key if 2FA is enabled.
        var secret = twoFactorEnabled
            ? Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20))
            : null;

        var user = new User
        {
            Username = $"test-{suffix}",
            Email = $"test-{suffix}@example.com",
            Role = UserRole.PublicActor,
            ActorType = "Municipality",
            CreatedAt = DateTime.UtcNow,
            TwoFactorEnabled = twoFactorEnabled,
            TwoFactorSecret = secret
        };

        // Hash password securely using ASP.NET Core IPasswordHasher.
        user.PasswordHash = passwordHasher.HashPassword(
            user,
            TestCredentials.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user;
    }
}

/// <summary>
/// Constant credentials used across integration test cases for deterministic user logins.
/// </summary>
public static class TestCredentials
{
    public const string Password = "Test-password-123!";
}