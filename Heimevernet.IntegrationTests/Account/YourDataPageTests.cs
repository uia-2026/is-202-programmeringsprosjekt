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
/// Integration tests for the "Your data" self-service page: access rules, own-activity scoping
/// and JSON export.
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

}
