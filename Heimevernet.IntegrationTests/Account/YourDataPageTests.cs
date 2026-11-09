using System.Net;
using System.Text.Json;
using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.IntegrationTests.Account;

/// <summary>
/// Integration tests for the "Your data" self-service page: access rules, own-activity scoping,
/// JSON export and account deletion.
/// </summary>
public sealed class YourDataPageTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public YourDataPageTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/YourData")]
    [InlineData("/YourData/Export")]
    public async Task AnonymousUser_IsRedirectedToLogin(string path)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? string.Empty);
    }

    [Theory]
    [InlineData(UserRole.PublicActor)]
    [InlineData(UserRole.ResourceProvider)]
    public async Task AnyLoggedInRole_SeesOwnAccountDetails(UserRole role)
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(role: role);
        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        using var response = await client.GetAsync("/YourData");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(user.Username, html);
        Assert.Contains(user.Email, html);
        Assert.Contains("Not enabled", html);
    }

    [Fact]
    public async Task Activity_ShowsOnlyTheUsersOwnEntries()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var otherUser = await data.CreateUserAsync();
        var ownMarker = $"own-{Guid.NewGuid():N}";
        var otherMarker = $"other-{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Needs.Add(new Need
            {
                UserId = user.Id,
                CategoryId = await data.GetCategoryIdAsync(),
                Title = "Activity test",
                Region = ownMarker,
                ContactPoint = "contact"
            });
            await db.SaveChangesAsync();
            var needId = await db.Needs.Where(n => n.Region == ownMarker).Select(n => n.Id).SingleAsync();

            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = user.Id,
                ActorName = user.Username,
                Action = AuditAction.NeedCreated,
                EntityType = "Need",
                EntityId = needId
            });
            db.AuditLogs.Add(new AuditLog
            {
                ActorUserId = otherUser.Id,
                ActorName = otherUser.Username,
                Action = AuditAction.NeedCreated,
                EntityType = "Need",
                EntityId = needId,
                Details = otherMarker
            });
            await db.SaveChangesAsync();
        }

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);
        var html = WebUtility.HtmlDecode(await (await client.GetAsync("/YourData")).Content.ReadAsStringAsync());

        Assert.Contains($"Need created — {ownMarker}", html);
        Assert.Contains("Signed in", html); // the login performed by AuthenticatedClientHelper
        Assert.DoesNotContain(otherUser.Username, html);
    }

    [Fact]
    public async Task Export_ReturnsJsonFileWithoutCredentials()
    {
        var data = new TestDataFactory(_factory.Services);
        var plainUser = await data.CreateUserAsync();
        await data.CreateNeedAsync(plainUser.Id);
        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, plainUser.Email);

        using var response = await client.GetAsync("/YourData/Export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(".json", response.Content.Headers.ContentDisposition?.FileName ?? string.Empty);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal(plainUser.Email, root.GetProperty("account").GetProperty("email").GetString());
        Assert.Equal(1, root.GetProperty("needs").GetArrayLength());
        Assert.Equal(0, root.GetProperty("resources").GetArrayLength());
        Assert.True(root.GetProperty("activity").GetArrayLength() >= 1);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("twoFactorSecret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(plainUser.PasswordHash, json);
    }

    [Fact]
    public async Task DeleteAccount_WithWrongConfirmation_KeepsAccount()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/YourData/DeleteAccount",
            "/YourData",
            new Dictionary<string, string> { ["confirmUsername"] = "not-my-name" });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/YourData", response.Headers.Location?.OriginalString ?? string.Empty);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.Users.AnyAsync(u => u.Id == user.Id));
    }

    [Theory]
    [InlineData(true)]  // user owns the need
    [InlineData(false)] // user owns the resource
    public async Task DeleteAccount_WithAssignedMatch_IsBlockedAndDeletesNothing(bool userOwnsNeed)
    {
        var data = new TestDataFactory(_factory.Services);
        var graph = await data.CreateMatchGraphAsync(
            matchStatus: MatchStatus.Assigned,
            needStatus: NeedStatus.Assigned);
        var user = userOwnsNeed ? graph.Owner : graph.Provider;
        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/YourData/DeleteAccount",
            "/YourData",
            new Dictionary<string, string> { ["confirmUsername"] = user.Username });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/YourData", response.Headers.Location?.OriginalString ?? string.Empty);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await db.Users.AnyAsync(u => u.Id == user.Id));
            Assert.True(await db.Needs.AnyAsync(n => n.Id == graph.NeedId));
            Assert.True(await db.Resources.AnyAsync(r => r.Id == graph.ResourceId));
            var match = await db.Matches.SingleAsync(m => m.Id == graph.MatchId);
            Assert.Equal(MatchStatus.Assigned, match.Status);
        }

        // Still signed in, and the page explains why.
        using var page = await client.GetAsync("/YourData");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains(
            "You have an active match in progress",
            WebUtility.HtmlDecode(await page.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task DeleteAccount_RemovesUserAndListings_KeepsAuditWithNullActor_AndSignsOut()
    {
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);
        var needId = await data.CreateNeedAsync(user.Id);
        var resourceId = await data.CreateResourceAsync(user.Id);
        var matchedNeedId = await data.CreateNeedWithMatchAsync(user.Id, provider.Id, MatchStatus.UnderReview);

        // A match the user created between two records they do not own (Restrict FK on MatchedByUser),
        // so it is not removed by any cascade and must be deleted explicitly.
        var otherNeedOwner = await data.CreateUserAsync();
        var otherNeedId = await data.CreateNeedAsync(otherNeedOwner.Id);
        var otherResourceId = await data.CreateResourceAsync(provider.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        long loginAuditId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Matches.Add(new Match
            {
                NeedId = otherNeedId,
                ResourceId = otherResourceId,
                MatchedByUserId = user.Id,
                Status = MatchStatus.UnderReview
            });
            await db.SaveChangesAsync();

            loginAuditId = await db.AuditLogs
                .Where(a => a.ActorUserId == user.Id && a.Action == AuditAction.LoginSucceeded)
                .Select(a => a.Id)
                .FirstAsync();
        }

        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/YourData/DeleteAccount",
            "/YourData",
            new Dictionary<string, string> { ["confirmUsername"] = user.Username });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? string.Empty);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.Users.AnyAsync(u => u.Id == user.Id));
            Assert.False(await db.Needs.AnyAsync(n => n.Id == needId || n.Id == matchedNeedId));
            Assert.False(await db.Resources.AnyAsync(r => r.Id == resourceId));
            Assert.False(await db.Matches.AnyAsync(m => m.MatchedByUserId == user.Id));
            Assert.True(await db.Users.AnyAsync(u => u.Id == provider.Id));
            Assert.True(await db.Users.AnyAsync(u => u.Id == otherNeedOwner.Id));
            Assert.True(await db.Needs.AnyAsync(n => n.Id == otherNeedId));
            Assert.True(await db.Resources.AnyAsync(r => r.Id == otherResourceId));

            var preserved = await db.AuditLogs.SingleAsync(a => a.Id == loginAuditId);
            Assert.Null(preserved.ActorUserId);
        }

        // The session cookie was cleared, so the page now requires login again.
        using var after = await client.GetAsync("/YourData");
        Assert.Equal(HttpStatusCode.Redirect, after.StatusCode);
        Assert.Contains("/Account/Login", after.Headers.Location?.OriginalString ?? string.Empty);
    }
}
