using System.Collections.Concurrent;
using Heimevernet.Data;
using Heimevernet.Infrastructure.Outbox;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Notifications;

/// <summary>
/// The atomic claim in <see cref="IOutboxRepository.TryClaimAsync"/> is what stops two workers from taking
/// the same message. These tests run it against MariaDB, with real concurrent connections.
/// Email is off in this host, so no worker touches the rows.
/// </summary>
public sealed class OutboxClaimTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public OutboxClaimTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TwoConcurrentClaims_OnlyOneSucceeds()
    {
        var now = DateTime.UtcNow;
        var ids = await SeedPendingAsync(count: 20, nextAttemptAt: now.AddMinutes(-1));

        foreach (var id in ids)
        {
            // Two scopes, so two DbContexts and two database connections
            var results = await Task.WhenAll(ClaimAsync(id, now), ClaimAsync(id, now));

            Assert.Single(results, claimed => claimed);
        }

        var rows = await LoadAsync(ids);
        Assert.All(rows, m =>
        {
            Assert.Equal(OutboxStatus.Processing, m.Status);
            Assert.Equal(1, m.Attempts);
        });
    }

    [Fact]
    public async Task ParallelWorkers_ClaimEveryMessageExactlyOnce()
    {
        var now = DateTime.UtcNow;
        var ids = await SeedPendingAsync(count: 40, nextAttemptAt: now.AddMinutes(-1));
        var claimed = new ConcurrentBag<long>();

        async Task WorkerAsync(int seed)
        {
            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var random = new Random(seed);

            foreach (var id in ids.OrderBy(_ => random.Next()))
            {
                if (await repository.TryClaimAsync(id, now, OutboxPolicy.Lease))
                    claimed.Add(id);
            }
        }

        await Task.WhenAll(Enumerable.Range(1, 4).Select(seed => Task.Run(() => WorkerAsync(seed))));

        Assert.Equal(ids.Order(), claimed.Order());
    }

    [Fact]
    public async Task ClaimedMessage_CanOnlyBeClaimedAgainAfterItsLeaseExpires()
    {
        var now = DateTime.UtcNow;
        var id = (await SeedPendingAsync(count: 1, nextAttemptAt: now.AddMinutes(-1)))[0];
        var duringLease = now.AddMinutes(1);
        var afterLease = now.Add(OutboxPolicy.Lease).AddSeconds(1);

        Assert.True(await ClaimAsync(id, now));

        Assert.False(await ClaimAsync(id, duringLease));
        Assert.DoesNotContain(id, await GetDueIdsAsync(duringLease));

        Assert.Contains(id, await GetDueIdsAsync(afterLease));
        Assert.True(await ClaimAsync(id, afterLease));

        var message = (await LoadAsync([id])).Single();
        Assert.Equal(OutboxStatus.Processing, message.Status);
        Assert.Equal(2, message.Attempts);
        Assert.Equal(afterLease.Add(OutboxPolicy.Lease), message.LockedUntil!.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task PendingMessage_IsNotClaimedBeforeItsNextAttempt()
    {
        var now = DateTime.UtcNow;
        var id = (await SeedPendingAsync(count: 1, nextAttemptAt: now.AddMinutes(5)))[0];

        Assert.DoesNotContain(id, await GetDueIdsAsync(now));
        Assert.False(await ClaimAsync(id, now));
        Assert.True(await ClaimAsync(id, now.AddMinutes(6)));
    }

    [Fact]
    public async Task DoneMessage_IsNeverClaimedAgain()
    {
        var now = DateTime.UtcNow;
        var id = (await SeedPendingAsync(count: 1, nextAttemptAt: now.AddMinutes(-1)))[0];

        Assert.True(await ClaimAsync(id, now));
        await WithRepositoryAsync(r => r.MarkDoneAsync(id, now));

        var muchLater = now.AddDays(1);
        Assert.DoesNotContain(id, await GetDueIdsAsync(muchLater));
        Assert.False(await ClaimAsync(id, muchLater));
    }

    [Fact]
    public async Task FailingMessage_IsRetriedWithDelays_ThenMarkedFailed()
    {
        var time = DateTime.UtcNow;
        var id = (await SeedPendingAsync(count: 1, nextAttemptAt: time.AddMinutes(-1)))[0];

        for (var attempt = 1; attempt <= OutboxPolicy.MaxAttempts; attempt++)
        {
            Assert.True(await ClaimAsync(id, time));
            await WithRepositoryAsync(r => r.MarkFailedOrRetryAsync(id, attempt, $"failure {attempt}", time));

            var message = (await LoadAsync([id])).Single();
            Assert.Equal(attempt, message.Attempts);
            Assert.Equal($"failure {attempt}", message.LastError);
            Assert.Null(message.LockedUntil);

            if (attempt < OutboxPolicy.MaxAttempts)
            {
                Assert.Equal(OutboxStatus.Pending, message.Status);
                Assert.Equal(time.Add(OutboxPolicy.DelayAfter(attempt)), message.NextAttemptAt, TimeSpan.FromMilliseconds(1));

                // Not due until the delay has passed
                Assert.False(await ClaimAsync(id, time.Add(OutboxPolicy.DelayAfter(attempt)).AddSeconds(-1)));
                time = message.NextAttemptAt;
            }
            else
            {
                Assert.Equal(OutboxStatus.Failed, message.Status);
                Assert.NotNull(message.ProcessedAt);
            }
        }

        Assert.False(await ClaimAsync(id, time.AddDays(1)));
    }

    // ─────────────────────────────────────────────────────────────

    private async Task<List<long>> SeedPendingAsync(int count, DateTime nextAttemptAt)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var messages = Enumerable.Range(0, count)
            .Select(_ => new OutboxMessage
            {
                Type = "ClaimTest",
                DedupKey = $"claim-test:{Guid.NewGuid():N}",
                Payload = "{}",
                NextAttemptAt = nextAttemptAt
            })
            .ToList();

        db.OutboxMessages.AddRange(messages);
        await db.SaveChangesAsync();

        return messages.Select(m => m.Id).ToList();
    }

    private Task<bool> ClaimAsync(long id, DateTime now) =>
        WithRepositoryAsync(r => r.TryClaimAsync(id, now, OutboxPolicy.Lease));

    private async Task<IReadOnlyList<long>> GetDueIdsAsync(DateTime now) =>
        await WithRepositoryAsync(r => r.GetDueIdsAsync(now, take: 1000));

    private async Task<T> WithRepositoryAsync<T>(Func<IOutboxRepository, Task<T>> action)
    {
        using var scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<IOutboxRepository>());
    }

    private async Task WithRepositoryAsync(Func<IOutboxRepository, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<IOutboxRepository>());
    }

    private async Task<List<OutboxMessage>> LoadAsync(IReadOnlyCollection<long> ids)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.OutboxMessages.AsNoTracking().Where(m => ids.Contains(m.Id)).ToListAsync();
    }
}
