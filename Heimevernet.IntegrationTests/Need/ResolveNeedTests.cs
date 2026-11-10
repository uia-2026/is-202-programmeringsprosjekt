using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.NeedTests;

/// <summary>
/// Integration tests verifying that resolving a Need is only allowed for its owner and only
/// when the Need is Assigned, and that its matches and audit log are updated accordingly.
/// </summary>
public sealed class ResolveNeedTests
    : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;
    private readonly TestDataFactory _data;

    public ResolveNeedTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
        _data = new TestDataFactory(factory.Services);
    }

    /// <summary>
    /// Verifies that the owner can resolve an Assigned Need: the Need becomes Resolved,
    /// assigned matches become Resolved, rejected and cancelled matches are left untouched,
    /// and the resolution is written to the audit log.
    /// </summary>
    [Fact]
    public async Task Resolve_OwnerWithAssignedNeed_ResolvesNeedAndUpdatesMatches()
    {
        // Arrange: Assigned Need with one assigned, one rejected and one cancelled match.
        // An Assigned Need has no pending matches, since they are cancelled when the Need becomes Assigned.
        var graph = await _data.CreateMatchGraphAsync(
            matchStatus: MatchStatus.Assigned,
            needStatus: NeedStatus.Assigned);

        var anotherProvider = await _data.CreateUserAsync(role: UserRole.ResourceProvider);
        var cancelledMatchId = await AddMatchForNeedAsync(graph, anotherProvider.Id, MatchStatus.Cancelled);
        var rejectedMatchId = await AddMatchForNeedAsync(graph, anotherProvider.Id, MatchStatus.Rejected);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, graph.Owner.Email);

        // Act
        using var response = await PostResolveAsync(client, graph.NeedId);

        // Assert: Expect HTTP 200 OK.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = await db.Needs.FindAsync(graph.NeedId);
        var assignedMatch = await db.Matches.FindAsync(graph.MatchId);
        var cancelledMatch = await db.Matches.FindAsync(cancelledMatchId);
        var rejectedMatch = await db.Matches.FindAsync(rejectedMatchId);

        Assert.Equal(NeedStatus.Resolved, need!.Status);
        Assert.Equal(MatchStatus.Resolved, assignedMatch!.Status);
        Assert.Equal(MatchStatus.Cancelled, cancelledMatch!.Status);
        Assert.Equal(MatchStatus.Rejected, rejectedMatch!.Status);

        // Verify the resolution is audited for both the Need and the assigned Match.
        Assert.True(await db.AuditLogs.AnyAsync(a =>
            a.Action == AuditAction.NeedResolved &&
            a.EntityType == "Need" &&
            a.EntityId == graph.NeedId &&
            a.ActorUserId == graph.Owner.Id));

        Assert.True(await db.AuditLogs.AnyAsync(a =>
            a.Action == AuditAction.MatchResolved &&
            a.EntityType == "Match" &&
            a.EntityId == graph.MatchId));
    }

    /// <summary>
    /// Verifies the business rule: a Need that has not been assigned help yet (New or UnderReview)
    /// cannot be resolved. Nothing is changed and nothing is audited.
    /// </summary>
    [Theory]
    [InlineData(NeedStatus.New)]
    [InlineData(NeedStatus.UnderReview)]
    public async Task Resolve_NeedNotAssigned_ReturnsConflictAndLeavesDataUnchanged(NeedStatus needStatus)
    {
        // Arrange: Need in a non-assigned status with a pending match.
        var graph = await _data.CreateMatchGraphAsync(
            matchStatus: MatchStatus.UnderReview,
            needStatus: needStatus);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, graph.Owner.Email);

        // Act
        using var response = await PostResolveAsync(client, graph.NeedId);

        // Assert: Expect HTTP 409 Conflict with the explanation for the user.
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(
            "Only assigned needs can be resolved.",
            await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = await db.Needs.FindAsync(graph.NeedId);
        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(needStatus, need!.Status);
        Assert.Equal(MatchStatus.UnderReview, match!.Status);
        Assert.False(await db.AuditLogs.AnyAsync(a =>
            a.Action == AuditAction.NeedResolved &&
            a.EntityId == graph.NeedId));
    }

    /// <summary>
    /// Verifies that resolving an already resolved Need returns 409 Conflict.
    /// </summary>
    [Fact]
    public async Task Resolve_AlreadyResolvedNeed_ReturnsConflict()
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync(
            matchStatus: MatchStatus.Resolved,
            needStatus: NeedStatus.Resolved);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, graph.Owner.Email);

        // Act
        using var response = await PostResolveAsync(client, graph.NeedId);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(
            "Need is already resolved.",
            await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// Verifies ownership: another PublicActor cannot resolve the Need, and its status is unchanged.
    /// </summary>
    [Fact]
    public async Task Resolve_AnotherPublicActor_ReturnsForbiddenAndPreservesStatus()
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync(
            matchStatus: MatchStatus.Assigned,
            needStatus: NeedStatus.Assigned);

        var anotherUser = await _data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, anotherUser.Email);

        // Act
        using var response = await PostResolveAsync(client, graph.NeedId);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = await db.Needs.FindAsync(graph.NeedId);
        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(NeedStatus.Assigned, need!.Status);
        Assert.Equal(MatchStatus.Assigned, match!.Status);
    }

    /// <summary>
    /// Verifies that resolving a non-existent Need returns 404 Not Found.
    /// </summary>
    [Fact]
    public async Task Resolve_NonexistentNeed_ReturnsNotFound()
    {
        // Arrange
        var user = await _data.CreateUserAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, user.Email);

        // Act
        using var response = await PostResolveAsync(client, int.MaxValue);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies role-based access control: ResourceProviders cannot resolve Needs.
    /// </summary>
    [Fact]
    public async Task Resolve_ResourceProvider_IsRedirectedToAccessDenied()
    {
        // Arrange
        var provider = await _data.CreateUserAsync(role: UserRole.ResourceProvider);

        using var client = await AuthenticatedClientHelper.CreateAsync(_factory, provider.Email);

        // Act
        using var response = await AntiforgeryHelper.PostFormAsync(
            client,
            "/api/needs/999999/resolve",
            "/Account/Login",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.ToString());
    }

    /// <summary>
    /// Verifies that anonymous requests are redirected to the Login view.
    /// </summary>
    [Fact]
    public async Task Resolve_AnonymousUser_RedirectsToLogin()
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
            "/api/needs/999999/resolve",
            "/Account/Login",
            new Dictionary<string, string>());

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
    }

    private static Task<HttpResponseMessage> PostResolveAsync(HttpClient client, int needId) =>
        AntiforgeryHelper.PostFormAsync(
            client,
            $"/api/needs/{needId}/resolve",
            "/Need/Create",
            new Dictionary<string, string>());

    /// <summary>
    /// Adds a match with the given status between the graph's Need and a new Resource owned by <paramref name="providerId"/>.
    /// </summary>
    private async Task<int> AddMatchForNeedAsync(MatchTestGraph graph, int providerId, MatchStatus status)
    {
        var resourceId = await _data.CreateResourceAsync(providerId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = new Match
        {
            NeedId = graph.NeedId,
            ResourceId = resourceId,
            MatchedByUserId = graph.Owner.Id,
            Quantity = 1,
            Status = status
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        return match.Id;
    }
}
