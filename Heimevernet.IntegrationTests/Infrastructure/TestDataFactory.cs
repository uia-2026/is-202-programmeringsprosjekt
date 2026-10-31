
using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public async Task<User> CreateUserAsync(
        bool twoFactorEnabled = false,
        CancellationToken cancellationToken = default,
        UserRole role = UserRole.PublicActor)
    {
        using var scope = _services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        var suffix = Guid.NewGuid().ToString("N");

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

    /// <summary>
    /// Seeds a standalone Need record.
    /// </summary>
    public async Task<int> CreateNeedAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var need = new Need
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
    /// Seeds a standalone Resource record.
    /// </summary>
    public async Task<int> CreateResourceAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var resource = new Resource
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
            AvailableFrom = DateTime.UtcNow.AddHours(-1),
            AvailableTo = null
        };

        db.Resources.Add(resource);
        await db.SaveChangesAsync(cancellationToken);

        return resource.Id;
    }

    /// <summary>
    /// Seeds a Need, Resource, and Match relational graph.
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
    /// Seeds a Need, Resource, and Match relational graph.
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

    // Match acceptance test data

    /// <summary>
    /// Seeds a Need, Resource, and Match graph for Match workflow tests.
    /// </summary>
    public async Task<MatchTestGraph> CreateMatchGraphAsync(
        int needQuantity = 1,
        int resourceQuantity = 1,
        int matchQuantity = 1,
        MatchStatus matchStatus = MatchStatus.UnderReview,
        NeedStatus needStatus = NeedStatus.UnderReview,
        ResourceStatus resourceStatus = ResourceStatus.Available,
        CancellationToken cancellationToken = default)
    {
        var owner = await CreateUserAsync(
            role: UserRole.PublicActor,
            cancellationToken: cancellationToken);

        var provider = await CreateUserAsync(
            role: UserRole.ResourceProvider,
            cancellationToken: cancellationToken);

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await GetCategoryIdAsync(cancellationToken);

        var need = new Need
        {
            UserId = owner.Id,
            CategoryId = categoryId,
            Title = $"Acceptance test need {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            Priority = NeedPriority.Medium,
            ContactPoint = "Test contact",
            QuantityNeeded = needQuantity,
            Status = needStatus
        };

        var resource = new Resource
        {
            UserId = provider.Id,
            CategoryId = categoryId,
            Title = $"Acceptance test resource {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            ContactPoint = "Test contact",
            QuantityOffered = resourceQuantity,
            Status = resourceStatus,
            AvailableFrom = DateTime.UtcNow.AddHours(-1)
        };

        var match = new Match
        {
            Need = need,
            Resource = resource,
            MatchedByUserId = owner.Id,
            Quantity = matchQuantity,
            Status = matchStatus
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync(cancellationToken);

        return new MatchTestGraph(
            need.Id,
            resource.Id,
            match.Id,
            owner,
            provider);
    }

    /// <summary>
    /// Adds an assigned match that consumes quantity from the specified Need.
    /// </summary>
    public async Task AddAssignedMatchForNeedAsync(
        MatchTestGraph graph,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var resource = await CreateResourceEntityAsync(
            db,
            graph.Provider.Id,
            cancellationToken);

        db.Matches.Add(new Match
        {
            NeedId = graph.NeedId,
            ResourceId = resource.Id,
            MatchedByUserId = graph.Owner.Id,
            Quantity = quantity,
            Status = MatchStatus.Assigned
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds an assigned match that consumes quantity from the specified Resource.
    /// </summary>
    public async Task AddAssignedMatchForResourceAsync(
        MatchTestGraph graph,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = await CreateNeedEntityAsync(
            db,
            graph.Owner.Id,
            cancellationToken);

        db.Matches.Add(new Match
        {
            NeedId = need.Id,
            ResourceId = graph.ResourceId,
            MatchedByUserId = graph.Owner.Id,
            Quantity = quantity,
            Status = MatchStatus.Assigned
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a pending match for the specified Need using another provider's Resource.
    /// </summary>
    public async Task<int> AddPendingMatchForNeedAsync(
        MatchTestGraph graph,
        int anotherProviderId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var resource = await CreateResourceEntityAsync(
            db,
            anotherProviderId,
            cancellationToken);

        var match = new Match
        {
            NeedId = graph.NeedId,
            ResourceId = resource.Id,
            MatchedByUserId = graph.Owner.Id,
            Quantity = 1,
            Status = MatchStatus.UnderReview
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync(cancellationToken);

        return match.Id;
    }

    /// <summary>
    /// Adds a pending match for the specified Resource using another owner's Need.
    /// </summary>
    public async Task<int> AddPendingMatchForResourceAsync(
        MatchTestGraph graph,
        int anotherOwnerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = await CreateNeedEntityAsync(
            db,
            anotherOwnerId,
            cancellationToken);

        var match = new Match
        {
            NeedId = need.Id,
            ResourceId = graph.ResourceId,
            MatchedByUserId = anotherOwnerId,
            Quantity = 1,
            Status = MatchStatus.UnderReview
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync(cancellationToken);

        return match.Id;
    }

    private async Task<Resource> CreateResourceEntityAsync(
        AppDbContext db,
        int providerId,
        CancellationToken cancellationToken)
    {
        var categoryId = await db.Categories
            .Select(c => c.Id)
            .FirstAsync(cancellationToken);

        var resource = new Resource
        {
            UserId = providerId,
            CategoryId = categoryId,
            Title = $"Additional resource {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            ContactPoint = "Test contact",
            QuantityOffered = 10,
            Status = ResourceStatus.Available,
            AvailableFrom = DateTime.UtcNow.AddHours(-1)
        };

        db.Resources.Add(resource);
        await db.SaveChangesAsync(cancellationToken);

        return resource;
    }

    private async Task<Need> CreateNeedEntityAsync(
        AppDbContext db,
        int ownerId,
        CancellationToken cancellationToken)
    {
        var categoryId = await db.Categories
            .Select(c => c.Id)
            .FirstAsync(cancellationToken);

        var need = new Need
        {
            UserId = ownerId,
            CategoryId = categoryId,
            Title = $"Additional need {Guid.NewGuid():N}",
            Description = "Test description",
            Latitude = 0,
            Longitude = 0,
            Region = "Test region",
            Priority = NeedPriority.Medium,
            ContactPoint = "Test contact",
            QuantityNeeded = 10,
            Status = NeedStatus.UnderReview
        };

        db.Needs.Add(need);
        await db.SaveChangesAsync(cancellationToken);

        return need;
    }
}

/// <summary>
/// Holds the persisted entities used by match acceptance integration tests.
/// </summary>
public sealed record MatchTestGraph(
    int NeedId,
    int ResourceId,
    int MatchId,
    User Owner,
    User Provider);

/// <summary>
/// Constant credentials used across integration test cases.
/// </summary>
public static class TestCredentials
{
    public const string Password = "Test-password-123!";
}
