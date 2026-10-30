using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Microsoft.EntityFrameworkCore;

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
        CancellationToken cancellationToken = default, UserRole role = UserRole.PublicActor)
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
            Role = role,
            ActorType = role == UserRole.PublicActor ? "Municipality" : "Farmer",
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

    public async Task<int> GetCategoryIdAsync(
    HeimevernetWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Categories
            .Select(c => c.Id)
            .FirstAsync();
    }

    /// <summary>
    /// Seeds a standalone Need record directly into the test database.
    /// </summary>
    public async Task<int> CreateNeedAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var need = new Heimevernet.Models.Need
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = $"Integration test need {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            Priority = NeedPriority.Medium,
            ContactPoint = "Test contact point",
            QuantityNeeded = 1,
            Status = NeedStatus.New
        };

        db.Needs.Add(need);
        await db.SaveChangesAsync(cancellationToken);

        return need.Id;
    }

    /// <summary>
    /// Seeds a standalone Resource record directly into the test database.
    /// </summary>
    public async Task<int> CreateResourceAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var resource = new Heimevernet.Models.Resource
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = $"Integration test resource {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            ContactPoint = "Test contact point",
            QuantityOffered = 1,
            Status = ResourceStatus.Available,
            AvailableFrom = DateTime.Now.AddHours(-1),
            AvailableTo = null
        };

        db.Resources.Add(resource);
        await db.SaveChangesAsync(cancellationToken);

        return resource.Id;
    }

    /// <summary>
    /// Seeds a Need, Resource, and Match relational graph into the test database.
    /// </summary>
    public async Task<int> CreateNeedWithMatchAsync(
        int ownerId,
        int providerId,
        MatchStatus matchStatus,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var need = new Need
        {
            UserId = ownerId,
            CategoryId = categoryId,
            Title = $"Integration test need {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            Priority = NeedPriority.Medium,
            ContactPoint = "Test contact point",
            QuantityNeeded = 1,
            Status = NeedStatus.New
        };

        var resource = new Resource
        {
            UserId = providerId,
            CategoryId = categoryId,
            Title = $"Integration test resource {Guid.NewGuid():N}",
            Description = "Test resource",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            ContactPoint = "Test contact point",
            Status = ResourceStatus.Available,
            QuantityOffered = 1,
            AvailableFrom = DateTime.UtcNow
        };

        var match = new Match
        {
            Need = need,
            Resource = resource,
            MatchedByUserId = ownerId,
            Status = matchStatus,
            Quantity = 1
        };

        db.Needs.Add(need);
        db.Resources.Add(resource);
        db.Matches.Add(match);

        await db.SaveChangesAsync(cancellationToken);

        return need.Id;
    }


    /// <summary>
    /// Seeds a Need, Resource, and Match relational graph into the test database.
    /// </summary>
    public async Task<int> CreateResourceWithMatchAsync(
        int providerId,
        int ownerId,
        MatchStatus matchStatus,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var need = new Need
        {
            UserId = ownerId,
            CategoryId = categoryId,
            Title = $"Integration test need {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            Priority = NeedPriority.Medium,
            ContactPoint = "Test contact point",
            QuantityNeeded = 1,
            Status = NeedStatus.New
        };

        var resource = new Resource
        {
            UserId = providerId,
            CategoryId = categoryId,
            Title = $"Integration test resource {Guid.NewGuid():N}",
            Description = "Test resource",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            ContactPoint = "Test contact point",
            Status = ResourceStatus.Available,
            QuantityOffered = 1,
            AvailableFrom = DateTime.UtcNow
        };

        var match = new Match
        {
            Need = need,
            Resource = resource,
            MatchedByUserId = ownerId,
            Status = matchStatus,
            Quantity = 1
        };

        db.Needs.Add(need);
        db.Resources.Add(resource);
        db.Matches.Add(match);

        await db.SaveChangesAsync(cancellationToken);

        return resource.Id;
    }


    /// <summary>
    /// Retrieves the ID of the first seeded category.
    /// </summary>
    public async Task<int> GetCategoryIdAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Categories
            .Select(c => c.Id)
            .FirstAsync(cancellationToken);
    }


    public async Task<int> CreateMapNeedAsync(
        int userId,
        double latitude,
        double longitude,
        NeedStatus status = NeedStatus.New,
        NeedPriority priority = NeedPriority.Medium,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var need = new Need
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = $"Map test need {Guid.NewGuid():N}",
            Description = "Map test description",
            Latitude = latitude,
            Longitude = longitude,
            Region = "Test region",
            Priority = priority,
            ContactPoint = "Test contact point",
            QuantityNeeded = 1,
            Status = status
        };

        db.Needs.Add(need);
        await db.SaveChangesAsync(cancellationToken);

        return need.Id;
    }

    public async Task<int> CreateMapResourceAsync(
        int userId,
        double latitude,
        double longitude,
        ResourceStatus status = ResourceStatus.Available,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var resource = new Resource
        {
            UserId = userId,
            CategoryId = categoryId,
            Title = $"Map test resource {Guid.NewGuid():N}",
            Description = "Map test description",
            Latitude = latitude,
            Longitude = longitude,
            Region = "Test region",
            ContactPoint = "Test contact point",
            Status = status,
            QuantityOffered = 1,
            AvailableFrom = DateTime.UtcNow
        };

        db.Resources.Add(resource);
        await db.SaveChangesAsync(cancellationToken);

        return resource.Id;
    }

}

/// <summary>
/// Constant credentials used across integration test cases for deterministic user logins.
/// </summary>
public static class TestCredentials
{
    public const string Password = "Test-password-123!";
}