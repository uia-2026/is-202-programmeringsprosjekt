using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.MatchTests;

/// <summary>
/// Integration tests verifying Match acceptance workflows, owner authorization,
/// state transitions, quantity allocation boundaries, and automatic cancellation of competing proposals.
/// </summary>
public sealed class MatchAcceptanceTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;
    private readonly TestDataFactory _data;

    public MatchAcceptanceTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
        _data = new TestDataFactory(factory.Services);
    }

    /// <summary>
    /// Verifies that the Resource owner can accept a valid proposal, transitioning the Match to <c>Assigned</c>,
    /// the Need to <c>Assigned</c>, and the Resource to <c>Busy</c>.
    /// </summary>
    [Fact]
    public async Task Accept_ValidProposal_AssignsMatch()
    {
        // Arrange: Seed match graph and authenticate client as the Resource provider (owner).
        var graph = await _data.CreateMatchGraphAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Submit match acceptance POST request.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Expect HTTP 302 Redirect.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Verify entity statuses updated in the database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);
        var need = await db.Needs.FindAsync(graph.NeedId);
        var resource = await db.Resources.FindAsync(graph.ResourceId);

        Assert.Equal(MatchStatus.Assigned, match!.Status);
        Assert.Equal(NeedStatus.Assigned, need!.Status);
        Assert.Equal(ResourceStatus.Busy, resource!.Status);
    }

    /// <summary>
    /// Verifies that attempting to accept a non-existent match ID returns an HTTP 404 Not Found response.
    /// </summary>
    [Fact]
    public async Task Accept_NonexistentMatch_ReturnsNotFound()
    {
        // Arrange: Authenticate a valid Resource Provider user.
        var provider = await _data.CreateUserAsync(
            role: UserRole.ResourceProvider);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, provider.Email);

        // Act: Attempt to accept non-existent match ID int.MaxValue.
        using var response = await PostAcceptAsync(client, int.MaxValue);

        // Assert: Expect HTTP 404 Not Found.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies ownership authorization: a user who is not the Resource owner cannot accept the match,
    /// resulting in a redirect to AccessDenied and leaving the Match status unchanged.
    /// </summary>
    [Fact]
    public async Task Accept_UserIsNotResourceOwner_RedirectsToAccessDenied()
    {
        // Arrange: Seed match graph and log in as a different Resource Provider.
        var graph = await _data.CreateMatchGraphAsync();

        var anotherProvider = await _data.CreateUserAsync(
            role: UserRole.ResourceProvider);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, anotherProvider.Email);

        // Act: Non-owner attempts to accept the match proposal.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Cookie Authentication intercepts Forbid() and redirects to AccessDenied.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/AccessDenied",
            response.Headers.Location?.OriginalString ?? string.Empty);

        // Verify match status remains UnderReview in the database.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(MatchStatus.UnderReview, match!.Status);
    }

    /// <summary>
    /// Verifies state preconditions: matches that are not in the <c>UnderReview</c> state
    /// (e.g. Rejected, Cancelled, Assigned, Resolved) cannot be accepted again and remain unchanged.
    /// </summary>
    /// <param name="status">The initial non-UnderReview match status being tested.</param>
    [Theory]
    [InlineData(MatchStatus.Rejected)]
    [InlineData(MatchStatus.Cancelled)]
    [InlineData(MatchStatus.Assigned)]
    [InlineData(MatchStatus.Resolved)]
    public async Task Accept_MatchIsNotUnderReview_DoesNotChangeMatch(
        MatchStatus status)
    {
        // Arrange: Seed match graph with specified non-UnderReview status.
        var graph = await _data.CreateMatchGraphAsync(
            matchStatus: status);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Post acceptance request.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Request redirects and match status remains unchanged.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(status, match!.Status);
    }

    /// <summary>
    /// Verifies business rule: if the parent Need is no longer open (e.g., Resolved),
    /// accepting a proposal is rejected and the Match status remains <c>UnderReview</c>.
    /// </summary>
    [Fact]
    public async Task Accept_NeedIsNoLongerOpen_DoesNotAssignMatch()
    {
        // Arrange: Seed match graph where the Need is already Resolved.
        var graph = await _data.CreateMatchGraphAsync(
            needStatus: NeedStatus.Resolved);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Attempt to accept proposal for resolved Need.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Redirect issued and match remains UnderReview.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(MatchStatus.UnderReview, match!.Status);
    }

    /// <summary>
    /// Verifies business rule: if the target Resource is marked <c>Busy</c>,
    /// proposal acceptance is rejected and the Match status remains <c>UnderReview</c>.
    /// </summary>
    [Fact]
    public async Task Accept_ResourceIsBusy_DoesNotAssignMatch()
    {
        // Arrange: Seed match graph where the Resource is marked Busy.
        var graph = await _data.CreateMatchGraphAsync(
            resourceStatus: ResourceStatus.Busy);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Attempt to accept proposal with busy resource.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Redirect issued and match status remains UnderReview.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(MatchStatus.UnderReview, match!.Status);
    }

    /// <summary>
    /// Verifies capacity constraints: accepting a proposal when the remaining needed quantity
    /// is already fully satisfied by prior assigned matches prevents the acceptance.
    /// </summary>
    [Fact]
    public async Task Accept_InsufficientRemainingNeedQuantity_DoesNotAssignMatch()
    {
        // Arrange: Seed graph with need quantity 1, and add a pre-existing assigned match covering the total need.
        var graph = await _data.CreateMatchGraphAsync(
            needQuantity: 1,
            resourceQuantity: 10,
            matchQuantity: 1);

        await _data.AddAssignedMatchForNeedAsync(
            graph,
            quantity: 1);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Attempt to accept second proposal exceeding need capacity.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Acceptance rejected and match remains UnderReview.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(MatchStatus.UnderReview, match!.Status);
    }

    /// <summary>
    /// Verifies capacity constraints: accepting a proposal when the remaining resource offered quantity
    /// is already fully exhausted by prior assigned matches prevents the acceptance.
    /// </summary>
    [Fact]
    public async Task Accept_InsufficientRemainingResourceQuantity_DoesNotAssignMatch()
    {
        // Arrange: Seed graph with resource quantity 1, and add a pre-existing assigned match consuming the resource.
        var graph = await _data.CreateMatchGraphAsync(
            needQuantity: 10,
            resourceQuantity: 1,
            matchQuantity: 1);

        await _data.AddAssignedMatchForResourceAsync(
            graph,
            quantity: 1);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Attempt to accept proposal exceeding offered resource capacity.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Acceptance rejected and match remains UnderReview.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(MatchStatus.UnderReview, match!.Status);
    }

    /// <summary>
    /// Verifies cascading cancellation rules: when accepting a proposal fully satisfies the Need's quantity,
    /// all other competing pending proposals linked to that Need are automatically transitioned to <c>Cancelled</c>.
    /// </summary>
    [Fact]
    public async Task Accept_NeedBecomesFullyAllocated_CancelsOtherPendingProposals()
    {
        // Arrange: Seed graph with need quantity 1 and add a competing pending proposal from another provider.
        var graph = await _data.CreateMatchGraphAsync(
            needQuantity: 1,
            resourceQuantity: 10,
            matchQuantity: 1);

        var anotherProvider = await _data.CreateUserAsync(
            role: UserRole.ResourceProvider);

        var pendingMatchId = await _data.AddPendingMatchForNeedAsync(
            graph,
            anotherProvider.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Accept primary proposal that fully satisfies the Need.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Primary match assigned and competing pending match automatically cancelled.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var need = await db.Needs.FindAsync(graph.NeedId);
        var acceptedMatch = await db.Matches.FindAsync(graph.MatchId);
        var pendingMatch = await db.Matches.FindAsync(pendingMatchId);

        Assert.Equal(NeedStatus.Assigned, need!.Status);
        Assert.Equal(MatchStatus.Assigned, acceptedMatch!.Status);
        Assert.Equal(MatchStatus.Cancelled, pendingMatch!.Status);
    }

    /// <summary>
    /// Verifies cascading cancellation rules: when accepting a proposal fully exhausts the Resource's available quantity,
    /// all other competing pending proposals targeting that Resource are automatically transitioned to <c>Cancelled</c>.
    /// </summary>
    [Fact]
    public async Task Accept_ResourceBecomesFullyAllocated_CancelsOtherPendingProposals()
    {
        // Arrange: Seed graph with resource quantity 1 and add a competing pending proposal targeting this resource.
        var graph = await _data.CreateMatchGraphAsync(
            needQuantity: 10,
            resourceQuantity: 1,
            matchQuantity: 1);

        var anotherOwner = await _data.CreateUserAsync(
            role: UserRole.PublicActor);

        var pendingMatchId = await _data.AddPendingMatchForResourceAsync(
            graph,
            anotherOwner.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act: Accept proposal that fully exhausts the Resource capacity.
        using var response = await PostAcceptAsync(client, graph.MatchId);

        // Assert: Primary match assigned, Resource set to Busy, and competing pending match cancelled.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var resource = await db.Resources.FindAsync(graph.ResourceId);
        var acceptedMatch = await db.Matches.FindAsync(graph.MatchId);
        var pendingMatch = await db.Matches.FindAsync(pendingMatchId);

        Assert.Equal(ResourceStatus.Busy, resource!.Status);
        Assert.Equal(MatchStatus.Assigned, acceptedMatch!.Status);
        Assert.Equal(MatchStatus.Cancelled, pendingMatch!.Status);
    }

    /// <summary>
    /// Helper method issuing a POST form request to <c>/Match/Accept</c> with anti-forgery token handling.
    /// </summary>
    private static Task<HttpResponseMessage> PostAcceptAsync(
        HttpClient client,
        int matchId)
    {
        return AntiforgeryHelper.PostFormAsync(
            client,
            $"/Match/Accept?id={matchId}",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["id"] = matchId.ToString()
            });
    }
}