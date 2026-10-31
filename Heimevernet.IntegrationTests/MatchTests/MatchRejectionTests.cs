
using Heimevernet.Data;
using Heimevernet.IntegrationTests.Infrastructure;
using Heimevernet.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Heimevernet.IntegrationTests.MatchTests;

/// <summary>
/// Integration tests verifying Match rejection workflows, owner authorization,
/// state transitions, and Need status recalculation.
/// </summary>
public sealed class MatchRejectionTests : IClassFixture<HeimevernetWebApplicationFactory>
{
    private readonly HeimevernetWebApplicationFactory _factory;
    private readonly TestDataFactory _data;

    public MatchRejectionTests(HeimevernetWebApplicationFactory factory)
    {
        _factory = factory;
        _data = new TestDataFactory(factory.Services);
    }

    /// <summary>
    /// Verifies that the Resource owner can reject a valid proposal,
    /// transitioning the Match to <c>Rejected</c> and the Need to <c>New</c>.
    /// </summary>
    [Fact]
    public async Task Reject_ValidProposal_RejectsMatch()
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act
        using var response = await PostRejectAsync(client, graph.MatchId);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);
        var need = await db.Needs.FindAsync(graph.NeedId);

        Assert.Equal(MatchStatus.Rejected, match!.Status);
        Assert.Equal(NeedStatus.New, need!.Status);
    }

    /// <summary>
    /// Verifies that attempting to reject a non-existent Match returns HTTP 404 Not Found.
    /// </summary>
    [Fact]
    public async Task Reject_NonexistentMatch_ReturnsNotFound()
    {
        // Arrange
        var provider = await _data.CreateUserAsync(
            role: UserRole.ResourceProvider);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, provider.Email);

        // Act
        using var response = await PostRejectAsync(client, int.MaxValue);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that a user who does not own the Resource cannot reject its Match.
    /// </summary>
    [Fact]
    public async Task Reject_UserIsNotResourceOwner_RedirectsToAccessDenied()
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync();

        var anotherProvider = await _data.CreateUserAsync(
            role: UserRole.ResourceProvider);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, anotherProvider.Email);

        // Act
        using var response = await PostRejectAsync(client, graph.MatchId);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Account/AccessDenied",
            response.Headers.Location?.OriginalString ?? string.Empty);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);
        var need = await db.Needs.FindAsync(graph.NeedId);

        Assert.Equal(MatchStatus.UnderReview, match!.Status);
        Assert.Equal(NeedStatus.UnderReview, need!.Status);
    }

    /// <summary>
    /// Verifies that Matches not in the <c>UnderReview</c> state cannot be rejected.
    /// </summary>
    /// <param name="status">The initial Match status being tested.</param>
    [Theory]
    [InlineData(MatchStatus.Rejected)]
    [InlineData(MatchStatus.Cancelled)]
    [InlineData(MatchStatus.Assigned)]
    [InlineData(MatchStatus.Resolved)]
    public async Task Reject_MatchIsNotUnderReview_DoesNotChangeMatch(
        MatchStatus status)
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync(
            matchStatus: status);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act
        using var response = await PostRejectAsync(client, graph.MatchId);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);

        Assert.Equal(status, match!.Status);
    }

    /// <summary>
    /// Verifies that rejecting a proposal does not reset the Need
    /// when another assigned Match still contributes to its allocation.
    /// </summary>
    [Fact]
    public async Task Reject_OtherAssignedMatchesExist_KeepsNeedUnderReview()
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync(
            needQuantity: 10,
            resourceQuantity: 10,
            matchQuantity: 1);

        await _data.AddAssignedMatchForNeedAsync(
            graph,
            quantity: 1);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act
        using var response = await PostRejectAsync(client, graph.MatchId);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);
        var need = await db.Needs.FindAsync(graph.NeedId);

        Assert.Equal(MatchStatus.Rejected, match!.Status);
        Assert.Equal(NeedStatus.UnderReview, need!.Status);
    }

    /// <summary>
    /// Verifies that rejecting a proposal does not reset the Need
    /// while another proposal for the same Need is still pending.
    /// </summary>
    [Fact]
    public async Task Reject_OtherPendingMatchesExist_KeepsNeedUnderReview()
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync();

        var anotherProvider = await _data.CreateUserAsync(
            role: UserRole.ResourceProvider);

        var pendingMatchId = await _data.AddPendingMatchForNeedAsync(
            graph,
            anotherProvider.Id);

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act
        using var response = await PostRejectAsync(client, graph.MatchId);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var rejectedMatch = await db.Matches.FindAsync(graph.MatchId);
        var pendingMatch = await db.Matches.FindAsync(pendingMatchId);
        var need = await db.Needs.FindAsync(graph.NeedId);

        Assert.Equal(MatchStatus.Rejected, rejectedMatch!.Status);
        Assert.Equal(MatchStatus.UnderReview, pendingMatch!.Status);
        Assert.Equal(NeedStatus.UnderReview, need!.Status);
    }

    /// <summary>
    /// Verifies that rejecting the last pending proposal resets the Need to <c>New</c>
    /// when no other assigned allocations or pending proposals exist.
    /// </summary>
    [Fact]
    public async Task Reject_NoOtherMatchesExist_ResetsNeedToNew()
    {
        // Arrange
        var graph = await _data.CreateMatchGraphAsync();

        using var client = await AuthenticatedClientHelper.CreateAsync(
            _factory, graph.Provider.Email);

        // Act
        using var response = await PostRejectAsync(client, graph.MatchId);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var match = await db.Matches.FindAsync(graph.MatchId);
        var need = await db.Needs.FindAsync(graph.NeedId);

        Assert.Equal(MatchStatus.Rejected, match!.Status);
        Assert.Equal(NeedStatus.New, need!.Status);
    }

    /// <summary>
    /// Helper method issuing a POST request to <c>/Match/Reject</c>
    /// with anti-forgery token handling.
    /// </summary>
    private static Task<HttpResponseMessage> PostRejectAsync(
        HttpClient client,
        int matchId)
    {
        return AntiforgeryHelper.PostFormAsync(
            client,
            $"/Match/Reject?id={matchId}",
            "/Account/Login",
            new Dictionary<string, string>
            {
                ["id"] = matchId.ToString()
            });
    }
}
