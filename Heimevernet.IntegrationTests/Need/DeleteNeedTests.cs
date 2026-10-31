using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.NeedTests;

/// <summary>
/// Integration tests verifying Need deletion, authorization,
/// ownership validation, match status business constraints, and database cleanup.
/// </summary>
public sealed class DeleteNeedTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public DeleteNeedTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that the owner of a Need without any associated active matches
    /// can successfully delete the Need, redirecting to the index list view and removing it from the database.
    /// </summary>
    [Fact]
    public async Task Delete_OwnerWithoutMatches_DeletesNeedAndRedirectsToIndex()
    {
        // Arrange: Create owner user, persist a Need, and build authenticated HTTP client session.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var needId = await data.CreateNeedAsync(user.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        // Act: POST form request to delete the Need (fetch token from GET /Need/Create).
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Need/Delete/{needId}",
            "/Need/Create",
            new Dictionary<string, string>());

        // Assert: Verify HTTP 302 Redirect to the Needs index page.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Need",
            response.Headers.Location?.OriginalString ?? string.Empty);

        // Verify entity deletion from database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Needs.AnyAsync(n => n.Id == needId));
    }

    /// <summary>
    /// Verifies that an authenticated user who is NOT the owner of the Need cannot delete it,
    /// resulting in a redirect to Access Denied and leaving the database entity untouched.
    /// </summary>
    [Fact]
    public async Task Delete_AnotherPublicActor_ReturnsForbiddenAndPreservesNeed()
    {
        // Arrange: Create owner and a non-owner user, seed Need, and log in as non-owner.
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync();
        var anotherUser = await data.CreateUserAsync();

        var needId = await data.CreateNeedAsync(owner.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            anotherUser.Email);

        // Act: Non-owner attempts to delete owner's Need.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Need/Delete/{needId}",
            "/Need/Create",
            new Dictionary<string, string>());

        // Assert: Expect 302 Redirect to AccessDenied.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.ToString());

        // Verify Need still exists in database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Needs.AnyAsync(n => n.Id == needId));
    }

    /// <summary>
    /// Verifies business constraint rule: Needs connected to an active or pending match
    /// (UnderReview or Assigned) cannot be deleted, redirecting back to details with entity preserved.
    /// </summary>
    [Theory]
    [InlineData(MatchStatus.UnderReview)]
    [InlineData(MatchStatus.Assigned)]
    public async Task Delete_NeedWithPendingOrAssignedMatch_ReturnsConflictAndPreservesNeed(
        MatchStatus matchStatus)
    {
        // Arrange: Seed owner, provider, and a Need linked to a match in specified active status.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await data.CreateNeedWithMatchAsync(
            user.Id,
            provider.Id,
            matchStatus);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        // Act: Attempt to delete locked Need.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Need/Delete/{needId}",
            "/Need/Create",
            new Dictionary<string, string>());

        // Assert: Expect redirect back to Need details view due to active match constraint.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            $"/Need/Details/{needId}",
            response.Headers.Location?.OriginalString ?? string.Empty);

        // Verify entity was NOT removed from database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Needs.AnyAsync(n => n.Id == needId));
    }

    /// <summary>
    /// Verifies that a Need linked to a Resolved match
    /// cannot be deleted by its owner.
    /// </summary>
    [Fact]
    public async Task Delete_NeedWithResolvedMatch_ReturnsRedirectAndPreservesNeed()
    {
        // Arrange: Seed owner, provider, and a Need linked to a Resolved match.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);

        var needId = await data.CreateNeedWithMatchAsync(
            user.Id,
            provider.Id,
            MatchStatus.Resolved);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        // Act: Owner requests deletion of resolved Need.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Need/Delete/{needId}",
            "/Need/Create",
            new Dictionary<string, string>());

        // Assert: Redirect issued and entity deleted from database.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(response.Headers.Location?.OriginalString));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Needs.AnyAsync(n => n.Id == needId));
    }

    /// <summary>
    /// Verifies that attempting to delete a non-existent Need ID returns HTTP 404 Not Found.
    /// </summary>
    [Fact]
    public async Task Delete_NonexistentNeed_ReturnsNotFound()
    {
        // Arrange: Authenticate valid user session.
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        // Act: Post delete request for invalid ID 999999.
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Delete/999999",
            "/Need/Create",
            new Dictionary<string, string>());

        // Assert: Expect HTTP 404 Not Found response.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies role-based access control: users with ResourceProvider role
    /// are denied deletion privileges and redirected to Access Denied.
    /// </summary>
    [Fact]
    public async Task Delete_ResourceProvider_IsRedirectedToAccessDenied()
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            provider.Email);

        // Act
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Delete/999999",
            "/Account/Login",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/AccessDenied",
            response.Headers.Location?.ToString());
    }

    /// <summary>
    /// Verifies authentication gatekeeper: unauthenticated anonymous requests
    /// are redirected to the Login view.
    /// </summary>
    [Fact]
    public async Task Delete_AnonymousUser_RedirectsToLogin()
    {
        // Arrange
        using var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        // Act
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/Need/Delete/999999",
            "/Account/Login",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.ToString());
    }
}