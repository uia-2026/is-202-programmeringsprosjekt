using System.Text.Json;
using Heimevernet.Data;
using Heimevernet.Exceptions;
using Heimevernet.Infrastructure.Notifications;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Notifications;

/// <summary>
/// Creating a need or resource writes its event to the outbox in the same transaction.
/// The worker is removed in this host, so the rows stay as they were written.
/// </summary>
public sealed class OutboxEnqueueTests : IClassFixture<NotificationsWebApplicationFactory>
{
    private readonly NotificationsWebApplicationFactory _factory;
    private readonly NotificationTestData _data;

    public OutboxEnqueueTests(NotificationsWebApplicationFactory factory)
    {
        _factory = factory;
        _data = new NotificationTestData(factory.Services);
    }

    [Fact]
    public async Task EnqueueingTheSameDedupKeyTwice_SecondCommitThrowsDuplicateRecordException()
    {
        var dedupKey = $"dedup-test:{Guid.NewGuid():N}";

        await EnqueueAndCommitAsync(dedupKey);

        // CriticalNeedRegisteredHandler and ResourceRegisteredHandler rely on exactly this exception
        await Assert.ThrowsAsync<DuplicateRecordException>(() => EnqueueAndCommitAsync(dedupKey));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.OutboxMessages.CountAsync(m => m.DedupKey == dedupKey));
    }

    [Fact]
    public async Task CreatingCriticalNeed_QueuesOneCriticalNeedRegisteredMessage()
    {
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);

        var needId = await CreateNeedAsync(owner.Id, NeedPriority.Critical);

        var message = Assert.Single(await LoadByDedupKeyAsync($"critical-need-registered:{needId}"));
        Assert.Equal(OutboxTypes.CriticalNeedRegistered, message.Type);
        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.RecipientUserId);

        var payload = JsonSerializer.Deserialize<CriticalNeedRegisteredPayload>(message.Payload);
        Assert.Equal(needId, payload!.NeedId);
    }

    [Theory]
    [InlineData(NeedPriority.Low)]
    [InlineData(NeedPriority.Medium)]
    public async Task CreatingNonCriticalNeed_QueuesNothing(NeedPriority priority)
    {
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);

        var needId = await CreateNeedAsync(owner.Id, priority);

        Assert.Empty(await LoadByDedupKeyAsync($"critical-need-registered:{needId}"));
    }

    [Fact]
    public async Task CreatingNeed_WhenTheOutboxWriteFails_SavesNeitherTheNeedNorTheMessage()
    {
        var owner = await _data.CreateUserAsync(UserRole.PublicActor);
        var title = $"Rolled back need {Guid.NewGuid():N}";
        var eventsBefore = await CountAsync(OutboxTypes.CriticalNeedRegistered);

        using (var scope = _factory.Services.CreateScope())
        {
            // The real NeedService, with an outbox that queues every message twice. The need is saved by the
            // first commit, and the duplicate key makes the second commit inside the same transaction fail.
            var outbox = new DuplicatingOutbox(scope.ServiceProvider.GetRequiredService<IOutbox>());
            var needService = ActivatorUtilities.CreateInstance<NeedService>(scope.ServiceProvider, outbox);

            var model = NeedModel(await FirstCategoryIdAsync(), NeedPriority.Critical, title);

            await Assert.ThrowsAsync<DuplicateRecordException>(() => needService.CreateAsync(model, owner.Id));
        }

        using var verificationScope = _factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Needs.AnyAsync(n => n.Title == title));
        Assert.Equal(eventsBefore, await CountAsync(OutboxTypes.CriticalNeedRegistered));
    }

    [Fact]
    public async Task CreatingResource_QueuesOneResourceRegisteredMessage()
    {
        var provider = await _data.CreateUserAsync(UserRole.ResourceProvider);

        int resourceId;
        using (var scope = _factory.Services.CreateScope())
        {
            var resourceService = scope.ServiceProvider.GetRequiredService<IResourceService>();
            resourceId = await resourceService.CreateAsync(ResourceModel(await FirstCategoryIdAsync()), provider.Id);
        }

        var message = Assert.Single(await LoadByDedupKeyAsync($"resource-registered:{resourceId}"));
        Assert.Equal(OutboxTypes.ResourceRegistered, message.Type);
        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Null(message.RecipientUserId);

        var payload = JsonSerializer.Deserialize<ResourceRegisteredPayload>(message.Payload);
        Assert.Equal(resourceId, payload!.ResourceId);
    }

    // ─────────────────────────────────────────────────────────────

    private async Task EnqueueAndCommitAsync(string dedupKey)
    {
        using var scope = _factory.Services.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        outbox.Enqueue("DedupTest", new { Test = true }, dedupKey);
        await unitOfWork.CommitAsync();
    }

    private async Task<int> CreateNeedAsync(int userId, NeedPriority priority)
    {
        using var scope = _factory.Services.CreateScope();
        var needService = scope.ServiceProvider.GetRequiredService<INeedService>();

        return await needService.CreateAsync(NeedModel(await FirstCategoryIdAsync(), priority), userId);
    }

    private async Task<int> FirstCategoryIdAsync() => (await _data.GetCategoryIdsAsync())[0];

    private static NeedCreateViewModel NeedModel(int categoryId, NeedPriority priority, string? title = null)
    {
        var at = NotificationTestData.NewArea();

        return new NeedCreateViewModel
        {
            CategoryId = categoryId,
            Title = title ?? $"Outbox need {Guid.NewGuid():N}",
            Latitude = at.Latitude,
            Longitude = at.Longitude,
            Region = "Test region",
            Priority = priority,
            ContactPoint = "Test contact point",
            QuantityNeeded = 1
        };
    }

    private static ResourceCreateViewModel ResourceModel(int categoryId)
    {
        var at = NotificationTestData.NewArea();

        return new ResourceCreateViewModel
        {
            CategoryId = categoryId,
            Title = $"Outbox resource {Guid.NewGuid():N}",
            Latitude = at.Latitude,
            Longitude = at.Longitude,
            Region = "Test region",
            AvailableFrom = DateTime.UtcNow.AddHours(-1),
            ContactPoint = "Test contact point",
            QuantityOffered = 1
        };
    }

    private async Task<List<OutboxMessage>> LoadByDedupKeyAsync(string dedupKey)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.OutboxMessages.AsNoTracking().Where(m => m.DedupKey == dedupKey).ToListAsync();
    }

    private async Task<int> CountAsync(string type)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.OutboxMessages.CountAsync(m => m.Type == type);
    }

    /// <summary>Queues every message twice with the same dedup key, so the commit that saves them fails.</summary>
    private sealed class DuplicatingOutbox(IOutbox inner) : IOutbox
    {
        public void Enqueue(string type, object payload, string dedupKey, int? recipientUserId = null)
        {
            inner.Enqueue(type, payload, dedupKey, recipientUserId);
            inner.Enqueue(type, payload, dedupKey, recipientUserId);
        }
    }
}
