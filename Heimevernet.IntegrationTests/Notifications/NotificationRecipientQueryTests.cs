using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Notifications;

/// <summary>
/// Runs the recipient searches against MariaDB. They combine a bounding box in SQL, enum values stored
/// as strings and grouping in memory, so a mock or the in-memory provider would not show real behaviour.
/// </summary>
public sealed class NotificationRecipientQueryTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private const double RadiusKm = 50;
    private const int MaxRecipients = 20;

    private readonly HeimevernetWebApplicationFactory _factory;
    private readonly NotificationTestData _data;

    public NotificationRecipientQueryTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
        _data = new NotificationTestData(factory.Services);
    }

    // ─────────────────────────────────────────────────────────────
    // Resources near a new critical need
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResourceRecipients_ReturnOneRowPerProvider_WithTheirNearestResource()
    {
        var area = NotificationTestData.NewArea();
        var needOwner = await _data.CreateUserAsync(UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);
        var otherProvider = await _data.CreateUserAsync(UserRole.ResourceProvider);

        await _data.CreateResourceAsync(provider.Id, area.North(20));
        var nearest = await _data.CreateResourceAsync(provider.Id, area.North(5));
        var other = await _data.CreateResourceAsync(otherProvider.Id, area.North(10));

        var recipients = await FindResourceRecipientsAsync(area, needOwner.Id);

        Assert.Collection(recipients,
            first =>
            {
                Assert.Equal(provider.Id, first.UserId);
                Assert.Equal(nearest.Title, first.ResourceTitle);
                Assert.Equal(5.0, first.DistanceKm, precision: 1);
            },
            second =>
            {
                Assert.Equal(otherProvider.Id, second.UserId);
                Assert.Equal(other.Title, second.ResourceTitle);
            });
    }

    [Theory]
    [InlineData("owned by the need owner")]
    [InlineData("owner opted out")]
    [InlineData("outside the radius")]
    [InlineData("busy")]
    [InlineData("expired")]
    [InlineData("other category")]
    [InlineData("available after the deadline")]
    public async Task ResourceRecipients_ExcludeResourcesThatDoNotFit(string reason)
    {
        var area = NotificationTestData.NewArea();
        var categories = await _data.GetCategoryIdsAsync();
        var now = DateTime.UtcNow;
        var deadline = now.AddDays(1);

        var needOwner = await _data.CreateUserAsync(UserRole.PublicActor);

        // A resource that fits, so an empty result cannot make the test pass by accident
        var control = await _data.CreateUserAsync(UserRole.ResourceProvider);
        await _data.CreateResourceAsync(control.Id, area.North(5));

        var misfit = reason switch
        {
            "owned by the need owner" => needOwner,
            "owner opted out" => await _data.CreateUserAsync(UserRole.ResourceProvider, emailNotifications: false),
            _ => await _data.CreateUserAsync(UserRole.ResourceProvider)
        };

        await _data.CreateResourceAsync(misfit.Id, area.North(reason == "outside the radius" ? 80 : 5), r =>
        {
            switch (reason)
            {
                case "busy": r.Status = ResourceStatus.Busy; break;
                case "expired": r.AvailableFrom = now.AddDays(-3); r.AvailableTo = now.AddHours(-1); break;
                case "other category": r.CategoryId = categories[1]; break;
                case "available after the deadline": r.AvailableFrom = deadline.AddDays(1); break;
            }
        });

        var recipients = await FindResourceRecipientsAsync(area, needOwner.Id, deadline, now);

        Assert.Contains(recipients, r => r.UserId == control.Id);
        Assert.DoesNotContain(recipients, r => r.UserId == misfit.Id);
    }

    [Fact]
    public async Task ResourceRecipients_AreLimitedToTheNearestProviders()
    {
        var area = NotificationTestData.NewArea();
        var needOwner = await _data.CreateUserAsync(UserRole.PublicActor);
        var providers = new List<User>();

        foreach (var km in new[] { 15.0, 5.0, 10.0 })
        {
            var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);
            await _data.CreateResourceAsync(provider.Id, area.North(km));
            providers.Add(provider);
        }

        var recipients = await FindResourceRecipientsAsync(area, needOwner.Id, maxRecipients: 2);

        Assert.Equal(new[] { providers[1].Id, providers[2].Id }, recipients.Select(r => r.UserId));
    }

    // ─────────────────────────────────────────────────────────────
    // Needs near a new resource
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task NeedRecipients_ReturnOneRowPerOwner_WithTheirNearestNeed()
    {
        var area = NotificationTestData.NewArea();
        var resourceOwner = await _data.CreateUserAsync(UserRole.ResourceProvider);
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);
        var otherOwner = await _data.CreateUserAsync(UserRole.PublicActor);

        await _data.CreateNeedAsync(owner.Id, area.North(20));
        var nearest = await _data.CreateNeedAsync(owner.Id, area.North(5));
        var other = await _data.CreateNeedAsync(otherOwner.Id, area.North(10));

        var recipients = await FindNeedRecipientsAsync(area, resourceOwner.Id);

        Assert.Collection(recipients,
            first =>
            {
                Assert.Equal(owner.Id, first.UserId);
                Assert.Equal(nearest.Id, first.NeedId);
                Assert.Equal(nearest.Title, first.NeedTitle);
                Assert.Equal(5.0, first.DistanceKm, precision: 1);
            },
            second =>
            {
                Assert.Equal(otherOwner.Id, second.UserId);
                Assert.Equal(other.Id, second.NeedId);
            });
    }

    [Theory]
    [InlineData(NeedPriority.Low)]
    [InlineData(NeedPriority.Medium)]
    [InlineData(NeedPriority.Critical)]
    public async Task NeedRecipients_IncludeOpenNeedsOfEveryPriority(NeedPriority priority)
    {
        var area = NotificationTestData.NewArea();
        var resourceOwner = await _data.CreateUserAsync(UserRole.ResourceProvider);
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);
        var need = await _data.CreateNeedAsync(owner.Id, area.North(5), n => n.Priority = priority);

        var recipients = await FindNeedRecipientsAsync(area, resourceOwner.Id);

        Assert.Equal(need.Id, Assert.Single(recipients).NeedId);
    }

    [Theory]
    [InlineData("owned by the resource owner")]
    [InlineData("owner opted out")]
    [InlineData("outside the radius")]
    [InlineData("resolved")]
    [InlineData("assigned")]
    [InlineData("past its deadline")]
    [InlineData("deadline before the resource is available")]
    [InlineData("other category")]
    public async Task NeedRecipients_ExcludeNeedsThatDoNotFit(string reason)
    {
        var area = NotificationTestData.NewArea();
        var categories = await _data.GetCategoryIdsAsync();
        var now = DateTime.UtcNow;

        // Only the last-but-one case needs a resource that starts in the future
        var availableFrom = reason == "deadline before the resource is available"
            ? now.AddDays(2)
            : now.AddDays(-2);

        var resourceOwner = await _data.CreateUserAsync(UserRole.ResourceProvider);

        var control = await _data.CreateUserAsync(UserRole.PublicActor);
        await _data.CreateNeedAsync(control.Id, area.North(5));

        var misfit = reason switch
        {
            "owned by the resource owner" => resourceOwner,
            "owner opted out" => await _data.CreateUserAsync(UserRole.PublicActor, emailNotifications: false),
            _ => await _data.CreateUserAsync(UserRole.PublicActor)
        };

        await _data.CreateNeedAsync(misfit.Id, area.North(reason == "outside the radius" ? 80 : 5), n =>
        {
            switch (reason)
            {
                case "resolved": n.Status = NeedStatus.Resolved; break;
                case "assigned": n.Status = NeedStatus.Assigned; break;
                case "past its deadline": n.Deadline = now.AddHours(-1); break;
                case "deadline before the resource is available": n.Deadline = now.AddDays(1); break;
                case "other category": n.CategoryId = categories[1]; break;
            }
        });

        var recipients = await FindNeedRecipientsAsync(area, resourceOwner.Id, availableFrom, now);

        Assert.Contains(recipients, r => r.UserId == control.Id);
        Assert.DoesNotContain(recipients, r => r.UserId == misfit.Id);
    }

    // ─────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<Heimevernet.DTOs.Notifications.NotificationRecipientDto>> FindResourceRecipientsAsync(
        GeoPoint needAt,
        int needOwnerId,
        DateTime? deadline = null,
        DateTime? now = null,
        int maxRecipients = MaxRecipients)
    {
        using var scope = _factory.Services.CreateScope();
        var resources = scope.ServiceProvider.GetRequiredService<IResourceRepository>();
        var categoryId = (await _data.GetCategoryIdsAsync())[0];

        return await resources.GetNotificationRecipientsAsync(
            categoryId, needAt.Latitude, needAt.Longitude, deadline, now ?? DateTime.UtcNow,
            needOwnerId, RadiusKm, maxRecipients);
    }

    private async Task<IReadOnlyList<NeedNotificationRecipientDto>> FindNeedRecipientsAsync(
        GeoPoint resourceAt,
        int resourceOwnerId,
        DateTime? availableFrom = null,
        DateTime? now = null)
    {
        using var scope = _factory.Services.CreateScope();
        var needs = scope.ServiceProvider.GetRequiredService<INeedRepository>();
        var categoryId = (await _data.GetCategoryIdsAsync())[0];
        var at = now ?? DateTime.UtcNow;

        return await needs.GetNotificationRecipientsAsync(
            categoryId, resourceAt.Latitude, resourceAt.Longitude, availableFrom ?? at.AddHours(-1), null, at,
            resourceOwnerId, RadiusKm, MaxRecipients);
    }
}
