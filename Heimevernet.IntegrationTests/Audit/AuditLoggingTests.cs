using System.Net;
using System.Net.Http.Json;
using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Audit;

public sealed class AuditLoggingTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public AuditLoggingTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatingNeed_PersistsAuditEntryWithGeneratedEntityId()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var categoryId = await data.GetCategoryIdAsync(_factory);
        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);
        var title = $"Audited need {Guid.NewGuid():N}";

        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Create",
            "/Need/Create",
            new Dictionary<string, string>
            {
                ["CategoryId"] = categoryId.ToString(),
                ["Title"] = title,
                ["Description"] = "Audit test",
                ["Latitude"] = "10",
                ["Longitude"] = "10",
                ["Region"] = "Test region",
                ["Priority"] = nameof(NeedPriority.Medium),
                ["ContactPoint"] = "Test contact",
                ["QuantityNeeded"] = "25"
            });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var need = await db.Needs.SingleAsync(n => n.Title == title);
        var audit = await db.AuditLogs.SingleAsync(a =>
            a.Action == AuditAction.NeedCreated &&
            a.EntityType == "Need" &&
            a.EntityId == need.Id);

        Assert.Equal(user.Id, audit.ActorUserId);
        Assert.DoesNotContain(user.Email, audit.Details ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownLogin_PersistsPrivacySafeFailedAuditEntry()
    {
        const string unknownEmail = "unknown-audit@example.com";
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Account/Login",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["Email"] = unknownEmail,
                ["Password"] = "not-recorded"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var audit = await db.AuditLogs
            .Where(a => a.Action == AuditAction.LoginFailed && a.ActorUserId == null)
            .OrderByDescending(a => a.Id)
            .FirstAsync();

        Assert.Equal("Unknown", audit.ActorName);
        Assert.DoesNotContain(unknownEmail, audit.Details ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditHistory_IsRestrictedToResourceProviders()
    {
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync();
        var needId = await data.CreateNeedAsync(owner.Id);
        var publicClient = await AuthenticatedClientHelper.CreateAsync(_factory, owner.Email);

        using var forbidden = await publicClient.GetAsync($"/api/audit/Need/{needId}");
        Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);

        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);
        using var authorizedClient = await AuthenticatedClientHelper.CreateAsync(_factory, provider.Email);
        using var authorized = await authorizedClient.GetAsync($"/api/audit/Need/{needId}");
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
    }

    [Fact]
    public async Task RollingBackBusinessTransaction_RollsBackAuditEntry()
    {
        using var scope = _factory.Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditLogger = scope.ServiceProvider.GetRequiredService<Heimevernet.Services.Interfaces.IAuditLogger>();
        var detailsMarker = $"rollback-{Guid.NewGuid():N}";

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            auditLogger.Record(AuditAction.NeedCreated, "Need", 999999, detailsMarker);
            await unitOfWork.SaveChangesAsync(ct);
            throw new InvalidOperationException("rollback test");
        }));

        using var verificationScope = _factory.Services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.AuditLogs.AnyAsync(a => a.Details == detailsMarker));
    }

    [Fact]
    public async Task CreatingNeed_RollsBackWhenAuditPersistenceFails()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var categoryId = await data.GetCategoryIdAsync(_factory);
        var title = $"Rolled back need {Guid.NewGuid():N}";

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var auditLogger = scope.ServiceProvider.GetRequiredService<Heimevernet.Services.Interfaces.IAuditLogger>();
        var need = new Need
        {
            UserId = user.Id,
            CategoryId = categoryId,
            Title = title,
            Region = "Agder",
            Priority = NeedPriority.Medium,
            ContactPoint = "contact",
            QuantityNeeded = 1
        };

        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            db.Needs.Add(need);
            await unitOfWork.SaveChangesAsync(ct);

            auditLogger.Record(
                AuditAction.NeedCreated,
                "Need",
                need.Id,
                actorUserId: int.MaxValue,
                actorName: "Invalid actor");
            await unitOfWork.SaveChangesAsync(ct);
        }));

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await verificationDb.Needs.AnyAsync(n => n.Title == title));
        Assert.False(await verificationDb.AuditLogs.AnyAsync(a => a.EntityType == "Need" && a.EntityId == need.Id));
    }

    [Fact]
    public async Task DeletingUser_SetsAuditActorToNullAndPreservesHistory()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        long auditId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var audit = new AuditLog
            {
                ActorUserId = user.Id,
                ActorName = user.Username,
                Action = AuditAction.LoginSucceeded,
                EntityType = "User",
                EntityId = user.Id
            };
            db.AuditLogs.Add(audit);
            await db.SaveChangesAsync();
            auditId = audit.Id;

            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }

        using var verificationScope = _factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var preserved = await verificationDb.AuditLogs.SingleAsync(a => a.Id == auditId);
        Assert.Null(preserved.ActorUserId);
        Assert.Equal(user.Username, preserved.ActorName);
    }
}
