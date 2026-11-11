using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Infrastructure;

/// <summary>
/// Seeds users, needs and resources at chosen positions for the notification tests.
/// Each test takes its own <see cref="NewArea"/>, hundreds of kilometres from every other area,
/// so rows from other tests in the same database never fall inside its search radius.
/// </summary>
public sealed class NotificationTestData
{
    private static int _nextArea;

    private readonly IServiceProvider _services;
    private readonly TestDataFactory _data;

    public NotificationTestData(IServiceProvider services)
    {
        _services = services;
        _data = new TestDataFactory(services);
    }

    /// <summary>
    /// A fresh area with whole-number coordinates. Areas are 5 degrees apart (over 400 km),
    /// far outside the 50 km radius.
    /// </summary>
    public static GeoPoint NewArea()
    {
        var n = Interlocked.Increment(ref _nextArea);
        return new GeoPoint(5 + n % 8 * 5, 60 + n / 8 * 5);
    }

    public async Task<User> CreateUserAsync(UserRole role, bool emailNotifications = true)
    {
        var user = await _data.CreateUserAsync(role: role);

        if (!emailNotifications)
        {
            await SetEmailNotificationsAsync(user.Id, false);
            user.EmailNotifications = false;
        }

        return user;
    }

    public async Task SetEmailNotificationsAsync(int userId, bool enabled)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailNotifications, enabled));
    }

    /// <summary>The seeded category ids, in a stable order. There are several, so tests can use a second one.</summary>
    public async Task<IReadOnlyList<int>> GetCategoryIdsAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Categories.OrderBy(c => c.Id).Select(c => c.Id).ToListAsync();
    }

    /// <summary>An open need in the first category, with no deadline, unless <paramref name="customize"/> changes it.</summary>
    public async Task<Need> CreateNeedAsync(int userId, GeoPoint at, Action<Need>? customize = null)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = new Need
        {
            UserId = userId,
            CategoryId = await db.Categories.OrderBy(c => c.Id).Select(c => c.Id).FirstAsync(),
            Title = $"Notification need {Guid.NewGuid():N}",
            Description = "Notification test",
            Latitude = at.Latitude,
            Longitude = at.Longitude,
            Region = "Test region",
            Priority = NeedPriority.Critical,
            ContactPoint = "Need contact point",
            QuantityNeeded = 1,
            Status = NeedStatus.New
        };

        customize?.Invoke(need);

        db.Needs.Add(need);
        await db.SaveChangesAsync();

        return need;
    }

    /// <summary>An available resource in the first category, open-ended, unless <paramref name="customize"/> changes it.</summary>
    public async Task<Resource> CreateResourceAsync(int userId, GeoPoint at, Action<Resource>? customize = null)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var resource = new Resource
        {
            UserId = userId,
            CategoryId = await db.Categories.OrderBy(c => c.Id).Select(c => c.Id).FirstAsync(),
            Title = $"Notification resource {Guid.NewGuid():N}",
            Description = "Notification test",
            Latitude = at.Latitude,
            Longitude = at.Longitude,
            Region = "Test region",
            ContactPoint = "Resource contact point",
            QuantityOffered = 1,
            Status = ResourceStatus.Available,
            AvailableFrom = DateTime.UtcNow.AddHours(-1),
            AvailableTo = null
        };

        customize?.Invoke(resource);

        db.Resources.Add(resource);
        await db.SaveChangesAsync();

        return resource;
    }
}

/// <summary>A position on the map, with a helper to move it a set distance north.</summary>
public readonly record struct GeoPoint(double Latitude, double Longitude)
{
    /// <summary>Kilometres per degree of latitude, using the 6371 km earth radius that GeoMath uses.</summary>
    private const double KmPerDegreeLatitude = 6371 * Math.PI / 180;

    public GeoPoint North(double km) => this with { Latitude = Latitude + km / KmPerDegreeLatitude };
}
