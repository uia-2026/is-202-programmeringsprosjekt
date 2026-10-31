
using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.ResourceTests;

/// <summary>
/// Integration tests verifying Resource deletion, authorization,
/// ownership validation, match constraints, and database cleanup.
/// </summary>
public sealed class DeleteResourceTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;

    public DeleteResourceTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that the owner can delete a Resource without associated matches.
    /// </summary>
    [Theory]
    [InlineData(UserRole.PublicActor)]
    [InlineData(UserRole.ResourceProvider)]
    public async Task Delete_OwnerWithoutMatches_DeletesResourceAndRedirectsToIndex(
        UserRole role)
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var user = await data.CreateUserAsync(role: role);
        var resourceId = await data.CreateResourceAsync(user.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            user.Email);

        // Act
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Resource/Delete/{resourceId}",
            "/Resource/Create",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Resource",
            response.Headers.Location?.OriginalString ?? string.Empty);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.False(await db.Resources.AnyAsync(r => r.Id == resourceId));
    }

    /// <summary>
    /// Verifies that a user cannot delete a Resource owned by another user.
    /// </summary>
    [Theory]
    [InlineData(UserRole.PublicActor)]
    [InlineData(UserRole.ResourceProvider)]
    public async Task Delete_AnotherUser_ReturnsForbiddenAndPreservesResource(
        UserRole anotherUserRole)
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var owner = await data.CreateUserAsync(role: UserRole.ResourceProvider);
        var anotherUser = await data.CreateUserAsync(role: anotherUserRole);

        var resourceId = await data.CreateResourceAsync(owner.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            anotherUser.Email);

        // Act
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Resource/Delete/{resourceId}",
            "/Resource/Create",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/AccessDenied",
            response.Headers.Location?.OriginalString ?? string.Empty);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Resources.AnyAsync(r => r.Id == resourceId));
    }

    /// <summary>
    /// Verifies that a Resource with any associated Match cannot be deleted,
    /// regardless of the Match status.
    /// </summary>
    [Theory]
    [InlineData(MatchStatus.UnderReview)]
    [InlineData(MatchStatus.Assigned)]
    [InlineData(MatchStatus.Resolved)]
    public async Task Delete_ResourceWithMatch_ReturnsConflictAndPreservesResource(
        MatchStatus matchStatus)
    {
        // Arrange
        var data = new TestDataFactory(_factory.Services);
        var provider = await data.CreateUserAsync(role: UserRole.ResourceProvider);
        var actor = await data.CreateUserAsync(role: UserRole.PublicActor);

        var resourceId = await data.CreateResourceWithMatchAsync(
            provider.Id,
            actor.Id,
            matchStatus);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory,
            provider.Email);

        // Act
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            $"/Resource/Delete/{resourceId}",
            "/Resource/Create",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            $"/Resource/Details/{resourceId}",
            response.Headers.Location?.OriginalString ?? string.Empty);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.True(await db.Resources.AnyAsync(r => r.Id == resourceId));
    }

    /// <summary>
    /// Verifies that deleting a non-existent Resource returns HTTP 404.
    /// </summary>
    [Fact]
    public async Task Delete_NonexistentResource_ReturnsNotFound()
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
            "/Resource/Delete/999999",
            "/Resource/Create",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that an anonymous user is redirected to Login.
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
            "/Resource/Delete/999999",
            "/Account/Login",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/Login",
            response.Headers.Location?.OriginalString ?? string.Empty);
    }
}
