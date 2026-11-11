using System.Reflection;
using Heimevernet.Data;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Notifications;

/// <summary>
/// The whole chain with real DI and a real database: create a need or resource, process the outbox the way
/// the worker does, and check what the fake email sender received. Catches wiring mistakes such as a
/// handler missing from DI, a wrong dedup key or a payload that does not round-trip.
/// </summary>
public sealed class NotificationDeliveryTests : IClassFixture<NotificationsWebApplicationFactory>
{
    private readonly NotificationsWebApplicationFactory _factory;
    private readonly NotificationTestData _data;

    public NotificationDeliveryTests(NotificationsWebApplicationFactory factory)
    {
        _factory = factory;
        _data = new NotificationTestData(factory.Services);
    }

    [Fact]
    public void EveryOutboxType_HasExactlyOneHandler()
    {
        var types = typeof(OutboxTypes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .Order();

        using var scope = _factory.Services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IOutboxHandler>().Select(h => h.Type).Order();

        Assert.Equal(types, handlers);

        // Builds the handler dictionary, which throws if two handlers share a type
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OutboxProcessor>());
    }

    [Fact]
    public async Task CriticalNeed_EmailsNearbyProviderOnce_WithLinkAndNoContactDetails()
    {
        var area = NotificationTestData.NewArea();
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);

        // Two fitting resources: the provider still gets one email, naming the nearest
        await _data.CreateResourceAsync(provider.Id, area.North(20));
        var nearest = await _data.CreateResourceAsync(provider.Id, area.North(3));

        // The owner's own resource nearby must not make them a recipient
        await _data.CreateResourceAsync(owner.Id, area.North(1));

        var model = NeedModel(await FirstCategoryIdAsync(), area, NeedPriority.Critical);
        var needId = await CreateNeedAsync(model, owner.Id);

        await _factory.DrainOutboxAsync();

        var email = Assert.Single(_factory.Emails.SentTo(provider.Email));
        Assert.Equal(provider.Username, email.ToName);
        Assert.Equal($"Critical need near you: {model.Title}", email.Subject);
        Assert.Contains($"{NotificationsWebApplicationFactory.PublicBaseUrl}/Need/Details/{needId}", email.Body);
        Assert.Contains(nearest.Title, email.Body);
        Assert.Contains("3 km", email.Body);
        Assert.DoesNotContain(model.ContactPoint, email.Body);
        Assert.DoesNotContain(owner.Email, email.Body);

        Assert.Empty(_factory.Emails.SentTo(owner.Email));

        Assert.Equal(OutboxStatus.Done, (await LoadAsync($"critical-need-registered:{needId}")).Status);
        Assert.Equal(OutboxStatus.Done, (await LoadAsync(CriticalNeedEmailKey(needId, provider.Id))).Status);
    }

    [Fact]
    public async Task NewResource_EmailsOwnerOfNearbyOpenNeed()
    {
        var area = NotificationTestData.NewArea();
        var needOwner = await _data.CreateUserAsync(UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);

        // Any priority counts for resource emails
        var need = await _data.CreateNeedAsync(needOwner.Id, area.North(4), n => n.Priority = NeedPriority.Low);

        var model = new ResourceCreateViewModel
        {
            CategoryId = await FirstCategoryIdAsync(),
            Title = $"Delivered resource {Guid.NewGuid():N}",
            Latitude = area.Latitude,
            Longitude = area.Longitude,
            Region = "Test region",
            AvailableFrom = DateTime.UtcNow.AddHours(-1),
            ContactPoint = "Resource contact point",
            QuantityOffered = 3
        };

        int resourceId;
        using (var scope = _factory.Services.CreateScope())
        {
            resourceId = await scope.ServiceProvider.GetRequiredService<IResourceService>().CreateAsync(model, provider.Id);
        }

        await _factory.DrainOutboxAsync();

        var email = Assert.Single(_factory.Emails.SentTo(needOwner.Email));
        Assert.Equal($"A resource that may fit your need: {model.Title}", email.Subject);
        Assert.Contains($"{NotificationsWebApplicationFactory.PublicBaseUrl}/Resource/Details/{resourceId}", email.Body);
        Assert.Contains(need.Title, email.Body);
        Assert.DoesNotContain(model.ContactPoint, email.Body);

        Assert.Empty(_factory.Emails.SentTo(provider.Email));
        Assert.Equal(OutboxStatus.Done,
            (await LoadAsync($"suitable-resource-email:{resourceId}:{needOwner.Id}")).Status);
    }

    [Fact]
    public async Task HandlingTheSameEventTwice_QueuesEachEmailOnce()
    {
        var area = NotificationTestData.NewArea();
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);
        await _data.CreateResourceAsync(provider.Id, area.North(5));

        var needId = await CreateNeedAsync(NeedModel(await FirstCategoryIdAsync(), area, NeedPriority.Critical), owner.Id);
        var eventKey = $"critical-need-registered:{needId}";
        var eventId = (await LoadAsync(eventKey)).Id;

        await _factory.ProcessAsync(eventId);

        // As if the worker stopped after the fan-out committed but before the event was marked Done
        await UpdateAsync(eventId, OutboxStatus.Pending);
        await _factory.ProcessAsync(eventId);

        var eventMessage = await LoadAsync(eventKey);
        Assert.Equal(OutboxStatus.Done, eventMessage.Status);
        Assert.Null(eventMessage.LastError);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.OutboxMessages.CountAsync(m => m.DedupKey == CriticalNeedEmailKey(needId, provider.Id)));
    }

    [Fact]
    public async Task OptingOutAfterTheEmailWasQueued_SkipsTheEmail()
    {
        var area = NotificationTestData.NewArea();
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);
        await _data.CreateResourceAsync(provider.Id, area.North(5));

        var needId = await CreateNeedAsync(NeedModel(await FirstCategoryIdAsync(), area, NeedPriority.Critical), owner.Id);

        // Fan out only, so the email message exists but is not sent yet
        await _factory.ProcessAsync((await LoadAsync($"critical-need-registered:{needId}")).Id);
        await _data.SetEmailNotificationsAsync(provider.Id, enabled: false);

        await _factory.DrainOutboxAsync();

        Assert.Empty(_factory.Emails.SentTo(provider.Email));
        Assert.Equal(OutboxStatus.Done, (await LoadAsync(CriticalNeedEmailKey(needId, provider.Id))).Status);
    }

    [Fact]
    public async Task ProviderAtTheDailyLimit_GetsNoMoreEmails()
    {
        var area = NotificationTestData.NewArea();
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);
        var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);
        await _data.CreateResourceAsync(provider.Id, area.North(5));

        // Five emails of the same type already handled in the last 24 hours (MaxEmailsPerUserPerDay = 5)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.OutboxMessages.AddRange(Enumerable.Range(0, 5).Select(_ => new OutboxMessage
            {
                Type = OutboxTypes.CriticalNeedEmail,
                DedupKey = $"daily-limit-test:{Guid.NewGuid():N}",
                RecipientUserId = provider.Id,
                Payload = "{}",
                Status = OutboxStatus.Done,
                ProcessedAt = DateTime.UtcNow.AddHours(-1)
            }));
            await db.SaveChangesAsync();
        }

        var needId = await CreateNeedAsync(NeedModel(await FirstCategoryIdAsync(), area, NeedPriority.Critical), owner.Id);

        await _factory.DrainOutboxAsync();

        Assert.Empty(_factory.Emails.SentTo(provider.Email));
        Assert.Equal(OutboxStatus.Done, (await LoadAsync(CriticalNeedEmailKey(needId, provider.Id))).Status);
    }

    // ─────────────────────────────────────────────────────────────

    private static string CriticalNeedEmailKey(int needId, int userId) => $"email-needs-resources:{needId}:{userId}";

    private async Task<int> FirstCategoryIdAsync() => (await _data.GetCategoryIdsAsync())[0];

    private static NeedCreateViewModel NeedModel(int categoryId, GeoPoint at, NeedPriority priority) => new()
    {
        CategoryId = categoryId,
        Title = $"Delivered need {Guid.NewGuid():N}",
        Latitude = at.Latitude,
        Longitude = at.Longitude,
        Region = "Test region",
        Priority = priority,
        ContactPoint = $"Need contact {Guid.NewGuid():N}",
        QuantityNeeded = 2
    };

    private async Task<int> CreateNeedAsync(NeedCreateViewModel model, int ownerId)
    {
        using var scope = _factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<INeedService>().CreateAsync(model, ownerId);
    }

    private async Task<OutboxMessage> LoadAsync(string dedupKey)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.OutboxMessages.AsNoTracking().SingleAsync(m => m.DedupKey == dedupKey);
    }

    private async Task UpdateAsync(long id, OutboxStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.OutboxMessages
            .Where(m => m.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, status)
                .SetProperty(m => m.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)));
    }
}
